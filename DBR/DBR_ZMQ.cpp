// DBR_ZMQ.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include <iostream>
#if defined(_WIN64) || defined(_WIN32)
#ifdef _WIN64
#pragma comment(lib, "./Lib/Windows/x64/DBRx64.lib")
#else
#pragma comment(lib, "./Lib/Windows/x86/DBRx86.lib")
#endif
#endif
#include "./Include/DynamsoftBarcodeReader.h"
#include "./Include/DynamsoftCommon.h"
#include <zmq.hpp>
#include <json/json.h>

using namespace dynamsoft::dbr;
int main()
{
    std::cout << "Hello World!\n";
    CBarcodeReader reader;
    reader.InitLicense("t0068MgAAAC4fm94YI4pg/vDZzcDxsfO4WUQlVyREaL48mf9dadZbFIwekHD31OA3s5J4iKjLJSomzNx3IwwB1CyAez714Ds=");
    std::cout << reader.GetVersion();

    struct stat buffer;
    if (stat("template.json", &buffer) == 0) { //use template
        char errorMessage[256];
        reader.InitRuntimeSettingsWithFile("template.json", CM_OVERWRITE, errorMessage, 256);
    }
    else {
        char sError[512];
        PublicRuntimeSettings runtimeSettings;
        reader.GetRuntimeSettings(&runtimeSettings);
        runtimeSettings.localizationModes[0] = LM_ONED_FAST_SCAN;     // Only use LM_LINES as the localization mode
        runtimeSettings.localizationModes[1] = LM_SKIP;
        runtimeSettings.localizationModes[2] = LM_SKIP;
        runtimeSettings.localizationModes[3] = LM_SKIP;
        reader.UpdateRuntimeSettings(&runtimeSettings, sError, 512);
    }
    zmq::context_t zmq_context(1);
    zmq::socket_t zmq_socket(zmq_context, ZMQ_REP);
    zmq_socket.bind("tcp://*:6666");
    while (true)
    {
        zmq::message_t recv_msg;
        zmq_socket.recv(recv_msg, zmq::recv_flags::none);
        std::string msg = recv_msg.to_string();
        std::cout << msg+"\n";
        try {
            struct stat buffer;
            if (stat(msg.c_str(), &buffer) == 0) {
                clock_t start, finish;
                double duration;
                start = clock();
                reader.DecodeFile(msg.c_str(), "");
                finish = clock();
                duration = (double)(finish - start) / CLOCKS_PER_SEC * 1000;
                TextResultArray* results = NULL;
                reader.GetAllTextResults(&results);
                std::string s = "";
                Json::Value rootValue = Json::objectValue;
                rootValue["results"] = Json::arrayValue;
                rootValue["elapsedTime"] = duration;
                for (int iIndex = 0; iIndex < results->resultsCount; iIndex++)
                {
                    PTextResult tr = results->results[iIndex];
                    Json::Value result = Json::objectValue;
                    result["barcodeText"] = tr->barcodeText;
                    result["barcodeFormat"] = tr->barcodeFormatString;
                    result["confidence"] = tr->results[0]->confidence;
                    result["x1"] = tr->localizationResult->x1;
                    result["x2"] = tr->localizationResult->x2;
                    result["x3"] = tr->localizationResult->x3;
                    result["x4"] = tr->localizationResult->x4;
                    result["y1"] = tr->localizationResult->y1;
                    result["y2"] = tr->localizationResult->y2;
                    result["y3"] = tr->localizationResult->y3;
                    result["y4"] = tr->localizationResult->y4;
                    rootValue["results"].append(result);
                }
                Json::StreamWriterBuilder builder;
                std::unique_ptr<Json::StreamWriter> writer(builder.newStreamWriter());
                std::ostringstream os;
                writer->write(rootValue, &os);
                s = os.str();
                //s = Json::FastWriter().write(rootValue);
                zmq::message_t response;
                response.rebuild(s.c_str(), s.length());
                zmq_socket.send(response, zmq::send_flags::dontwait);
                CBarcodeReader::FreeTextResults(&results);
            }
            else {
                zmq_socket.send(zmq::str_buffer("Received"), zmq::send_flags::dontwait);
            }
        }
        catch (...){
            std::cout << "error";
            zmq_socket.send(zmq::str_buffer("Error"), zmq::send_flags::dontwait);
        }
        
        if (msg == "q") {
            break;
        }
    }
}

// Run program: Ctrl + F5 or Debug > Start Without Debugging menu
// Debug program: F5 or Debug > Start Debugging menu

// Tips for Getting Started: 
//   1. Use the Solution Explorer window to add/manage files
//   2. Use the Team Explorer window to connect to source control
//   3. Use the Output window to see build output and other messages
//   4. Use the Error List window to view errors
//   5. Go to Project > Add New Item to create new code files, or Project > Add Existing Item to add existing code files to the project
//   6. In the future, to open this project again, go to File > Open > Project and select the .sln file
