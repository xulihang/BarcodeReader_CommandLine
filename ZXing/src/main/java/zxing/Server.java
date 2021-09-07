package zxing;
import java.awt.image.BufferedImage;
import java.io.File;
import java.io.IOException;
import java.util.ArrayList;
import java.util.Base64;
import java.util.HashMap;
import java.util.Map;
import com.alibaba.fastjson.JSON;
import javax.imageio.ImageIO;

import org.zeromq.SocketType;
import org.zeromq.ZMQ;

import com.google.zxing.*;
import com.google.zxing.client.j2se.BufferedImageLuminanceSource;
import com.google.zxing.common.HybridBinarizer;
import com.google.zxing.multi.qrcode.QRCodeMultiReader;

import org.zeromq.ZContext;

public class Server {
    private static Reader reader = null;
	public static void main(String[] args) throws IOException {
		// TODO Auto-generated method stub
		if (args.length==1) {
			//String filepath = "C:\\Users\\admin\\Pictures\\black_qr_code.png";
			String filepath = args[0];
	        File f = new File(filepath);
	        if (f.exists()) {
				String json = decodeFile(f);
				System.out.println(json);
	        }
			return;
		}else {
			System.out.println("Running under server mode.");
			try (ZContext context = new ZContext()) {
		    	// Socket to talk to clients
		    	ZMQ.Socket socket = context.createSocket(SocketType.REP);
		    	socket.bind("tcp://*:5557");

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
                    	String json = decodeFile(f);
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
	
	private static void initReaderIfNeeded() {
		if (reader == null) {
			reader = new QRCodeMultiReader();
		}
	}
	
	private static String decodeFile(File f) throws IOException {
		initReaderIfNeeded();
		Map<DecodeHintType,?> hints = new HashMap<>();
        hints.put(DecodeHintType.TRY_HARDER,null);
        long time0 = System.nanoTime();
        BufferedImage image = ImageIO.read(f);
        LuminanceSource source = new BufferedImageLuminanceSource(image);
        BinaryBitmap bitmap = new BinaryBitmap(new HybridBinarizer(source));
        
        ArrayList<HashMap<String,Object>> resultList = new ArrayList<>();
        HashMap<String,Object> DecodingResult = new HashMap<String, Object>();
        
        Result[] results = null;
        try {
            results = ((QRCodeMultiReader) reader).decodeMultiple(bitmap,hints);
            for (Result result:results) {
            	HashMap<String,Object> resultMap = new HashMap<String, Object>();
            	resultMap.put("barcodeText", result.getText());
            	resultMap.put("barcodeFormat", result.getBarcodeFormat().name());
            	String base64 = Base64.getEncoder().encodeToString(result.getRawBytes());
            	resultMap.put("rawbytes", base64);
            	float minX,maxX,minY,maxY;
            	ResultPoint[] points = result.getResultPoints();
            	minX = points[0].getX();
            	minY = points[0].getY();
            	maxX = 0;
            	maxY = 0;
            	for (ResultPoint point: points) {
            		minX=Math.min(minX, point.getX());
            		minY=Math.min(minY, point.getY());
            		maxX=Math.max(maxX, point.getX());
            		maxY=Math.max(maxY, point.getY());
            	}
            	resultMap.put("x1", minX);
            	resultMap.put("y1", minY);
            	resultMap.put("x2", maxX);
            	resultMap.put("y2", minY);
            	resultMap.put("x3", maxX);
            	resultMap.put("y3", maxY);
            	resultMap.put("x4", minX);
            	resultMap.put("y4", maxY);
            	resultList.add(resultMap);
            }
        } catch (NotFoundException e) {
            // fall thru, it means there is no QR code in image
        }
        long time1 = System.nanoTime();
        double milliseconds = (time1-time0)*1e-6;
        DecodingResult.put("results", resultList);
        DecodingResult.put("elapsedTime", milliseconds);
        String jsonString = JSON.toJSONString(DecodingResult);
        return jsonString;
	}

}
