import com.accusoft.barcodexpress.*;
import com.alibaba.fastjson.JSON;

import javax.imageio.ImageIO;

import org.zeromq.SocketType;
import org.zeromq.ZContext;
import org.zeromq.ZMQ;

import java.awt.image.BufferedImage;
import java.io.File;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;

public class ReadBarcodes {
    public static void main(String[] args) {
        BarcodeXpress barcodeXpress = new BarcodeXpress();

        // The SetSolutionName, SetSolutionKey and possibly the SetOEMLicenseKey method must be
        // called to distribute the runtime.  Note that the SolutionName, SolutionKey and
        // OEMLicenseKey values shown below are only examples.
        // BarcodeXpress.setSolutionName("YourSolutionName");
        // BarcodeXpress.setSolutionKey(1234,1234,1234,1234);
        // BarcodeXpress.setOemLicenseKey("2.0.YourOEMLicenseKeyGoesHere");

        BarcodeReader reader = barcodeXpress.getReader();
        List<BarcodeType> barcodeTypes = new ArrayList<>();
        barcodeTypes.add(BarcodeType.QRCODE);
        
        // Set scan barcode types
        reader.setBarcodeTypes(barcodeTypes.toArray(new BarcodeType[barcodeTypes.size()]));

        if (args.length==1) {
			//String filepath = "C:\\Users\\admin\\Pictures\\black_qr_code.png";
			String filepath = args[0];
	        File f = new File(filepath);
	        if (f.exists()) {
				String json = decodeFile(reader, f);
				System.out.println(json);
	        }
			return;
		}else {
			System.out.println("Running under server mode.");
			try (ZContext context = new ZContext()) {
		    	// Socket to talk to clients
		    	ZMQ.Socket socket = context.createSocket(SocketType.REP);
		    	socket.bind("tcp://*:5558");

	            while (true) {
	                // Block until a message is received
	                byte[] reply = socket.recv(0);
                    String s = new String(reply, ZMQ.CHARSET);
	                // Print the message
	                System.out.println(
	                    "Received: [" + s + "]"
	                );
	                String response = "Received";
                    File f = new File(s);
                    if (f.exists()) {
                    	String json = decodeFile(reader, f);
                    	response = json;
                    }
	                socket.send(response.getBytes(ZMQ.CHARSET), 0);
	                
	                if (s == "q") {
	                	break;
	                }
	            }
	        }
		}
    }

    public static String decodeFile(BarcodeReader reader, File inputFile) {
    	ArrayList<HashMap<String,Object>> resultList = new ArrayList<>();
        HashMap<String,Object> DecodingResult = new HashMap<String, Object>();
        long time0 = System.nanoTime();
        try {
            BufferedImage bufferedImage = ImageIO.read(inputFile);
            
            Result[] results = reader.analyze(bufferedImage);
            for (int i = 0; i < results.length; i++) {
                Result result = results[i];
                
                HashMap<String,Object> resultMap = new HashMap<String, Object>();
            	resultMap.put("barcodeText", result.getValue());
            	resultMap.put("barcodeFormat", result.getType().name());
            	resultMap.put("confidence", result.getConfidence());
            	resultMap.put("x1", result.getPoint1().x);
            	resultMap.put("y1", result.getPoint1().y);
            	resultMap.put("x2", result.getPoint2().x);
            	resultMap.put("y2", result.getPoint2().y);
            	resultMap.put("x3", result.getPoint3().x);
            	resultMap.put("y3", result.getPoint3().y);
            	resultMap.put("x4", result.getPoint4().x);
            	resultMap.put("y4", result.getPoint4().y);
            	resultList.add(resultMap);
            }
        }
        catch (Exception ex) {
            System.out.println(ex.getMessage());
        }
        long time1 = System.nanoTime();
        long milliseconds = (long) ((time1-time0)*1e-6);
        DecodingResult.put("results", resultList);
        DecodingResult.put("elapsedTime", milliseconds);
        String jsonString = JSON.toJSONString(DecodingResult);
        return jsonString;
    }
}
