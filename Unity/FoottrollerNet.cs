using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;
#if PLATFORM_ANDROID
using UnityEngine.Android;
#endif

public class FoottrollerNet : MonoBehaviour
{
    // Start is called before the first frame update
    public bool udpdatareceived;
    public string serverip;
    public static FoottrollerNet instance = null;
    public string localIPs;
    public int senderport=0;
    public int listenport;
    float broadcastMs, prebroadcastMs;
    public float udpheartbeatMs;
    float udpidleMs;
    int server_data_sink_ready = 0;
    
    // Receiving
    // receiving Thread
    Thread receiveThread;
    public bool udpConnReady;
    public bool udpReconnflag;
    // udpclient object
    UdpClient client;
    // public
    // public string IP = "127.0.0.1"; default local
    int port; // define > init
    string lastReceivedUDPPacket = "";

    // sending 
    // prefs
    private string IP_S;  // define in init
    int port_S;  // define in init

    // "connection" things
    IPEndPoint remoteEndPoint;
    IPEndPoint broadcastEndPoint;
    UdpClient client_S;
    public float lastupdatems;

    private byte[] udpBufsend;
    private int udpData_len;
    bool reinitudpflag;
    private int reinitcnt;
	public float joystick_x;
	public float joystick_y;
    public float RFheading;
    public float RFtilt;
    public float LFheading;
    public float LFtilt;
    public int TSLF;
    public int TSRF;
    public byte Byte1Data;
    public byte Byte2Data;
    public byte Byte4Data;

    public byte pre_Byte1Data;
    public byte pre_Byte2Data;
    
    byte pre_Byte3Data;
    public byte pre_Byte4Data;
    byte pre_Byte5Data;
    byte pre_Byte6Data;
    byte pre_Byte7Data;
    byte pre_Byte8Data;
    public bool testflag;

    int ipdix;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    void Start()
    {
        //Debug.Log("Ini udp loop");
        //senderport = 15000;
        udpConnReady = false;
        ipdix = 0;
        localIPs = LocalIPAddress();
        localIPs = localIPs + "255";
        init();
        init_S();
        ipdix = 0;
        udpdatareceived = false;
        serverip = "";
        reinitudpflag = false;
		prebroadcastMs=0;
		broadcastMs=0;
        udpReconnflag = false;

#if PLATFORM_ANDROID
       
        if (!Permission.HasUserAuthorizedPermission("android.permission.INTERNET"))
        {
            Permission.RequestUserPermission("android.permission.INTERNET");
        }
        Permission.RequestUserPermission("android.permission.INTERNET");
        if (!Permission.HasUserAuthorizedPermission("android.permission.ACCESS_WIFI_STATE"))
        {
            Permission.RequestUserPermission("android.permission.ACCESS_WIFI_STATE");
        }
#endif
    }

    // Update is called once per frame
    void Update()
    {
        lastupdatems = lastupdatems + Time.deltaTime;

        if (udpReconnflag) {
            broadcastMs = broadcastMs + Time.deltaTime;
            if (broadcastMs - prebroadcastMs > 5) {
                prebroadcastMs = broadcastMs;
                
                udpBufsend[0] = 1;            // request for server connection
                udpBufsend[1] = 2;            // connection type 2
                udpData_len = 2;
                // Den message zum Remote-Client senden.
                IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(serverip), 45041);
                // client_S.Send(udpBufsend, udpData_len, serverEndPoint);
                // Debug.Log("101 message sent: " + scanip);

                try
                {
                    client_S.Send(udpBufsend, udpData_len, serverEndPoint);
                }
                catch (Exception err)
                {
                    // print(err.ToString());
                }
            }
            
        }

        if (!udpConnReady)
        {
            broadcastMs = broadcastMs + Time.deltaTime;
            if (broadcastMs - prebroadcastMs> 5)
            {
				prebroadcastMs=broadcastMs;
				
                udpBufsend[0] = 1;
                udpData_len = 2;
                try
                {
                    //if (message != "")
                    //{

                    // Daten mit der UTF8-Kodierung in das Binärformat kodieren.
                    udpBufsend[0] = 1;            // request for server connection
                    udpBufsend[1] = 2;            // connection type 2
                    udpData_len = 2;
                    // Den message zum Remote-Client senden.
                    localIPs = LocalIPAddress();
                    string scanip="";
                    for (int idx = 1; idx <= 254; idx++) { 
                        int temp = idx;
                        scanip = localIPs + temp.ToString();
                        broadcastEndPoint = new IPEndPoint(IPAddress.Parse(scanip), port_S);
                        client_S.Send(udpBufsend, udpData_len, broadcastEndPoint);
                        // Debug.Log("101 message sent: " + scanip);
                    }
                    
                    //}
					
                    reinitudpflag = false;
                    reinitcnt = 0;
                }
                catch (Exception err)
                {
                    // print(err.ToString());
                    reinitudpflag = true;
                    reinitcnt = reinitcnt + 1;
                }

                //client_S.Send(udpBufsend, udpData_len, "192.168.1.112",12309);
            }
            RFheading = 0;
            LFheading = 0;
            RFtilt = 0;
            LFtilt = 0;
            TSLF = 0;
            TSRF = 0;

            if (reinitudpflag&&reinitcnt>10) {
                //init();
                //init_S();
                reinit();
                reinitudpflag = false;
            }

        }
        else {  // send regular heart beat back to server to maintain connection
            if (!udpReconnflag)
            {
                udpheartbeatMs = udpheartbeatMs + Time.deltaTime;
                if (udpheartbeatMs > 5 && udpConnReady)
                {
                    // ----------------------------
                    // Senden
                    // ----------------------------
                    IPAddress IP;
                    bool flag = IPAddress.TryParse(serverip, out IP);

                    if (flag) {
                        IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(serverip), 45041);
                        udpBufsend[0] = 1;   // heart beat message 102
                        udpBufsend[1] = 21;
                        udpData_len = 2;
                        try
                        {
                            //if (message != "")
                            //{

                            // Daten mit der UTF8-Kodierung in das Binärformat kodieren.
                            // Den message zum Remote-Client senden.
                            client_S.Send(udpBufsend, udpData_len, serverEndPoint);
                            //}
                            reinitcnt = 0;
                        }
                        catch (Exception err)
                        {
                            // print(err.ToString());
                            reinitudpflag = true;
                            reinitcnt = reinitcnt + 1;
                        }
                        udpheartbeatMs = 0;
                    }
                }
                udpidleMs = 0;




            }
        }
        if (reinitudpflag&&reinitcnt>5) {
            reinit();
            reinitudpflag = false;
        }
        
        if (lastupdatems > 8 && udpConnReady) {

            udpReconnflag = true;
            // udpConnReady = false;

            reinit();
            reinitudpflag = false;
            //lastupdatems = 0;
            // udpConnReady = false;
        }

        float temp_val = FoottrollerCtrl.instance.control_angle; // / 1.57f;
        if (temp_val > 1)
        {
            temp_val = 1;
        }
        else if (temp_val < -1)
        {
            temp_val = -1;
        }

        int tempval = 0;
        tempval = (int) (temp_val * 128 + 128f);
        if (tempval > 255)
        {
            tempval = 255;
        }
        else if (tempval < 0)
        {
            tempval = 0;
        }
        Byte1Data = (byte) tempval;

        tempval = 0;
        if (FoottrollerCtrl.instance.btnA)
        {
            tempval = tempval + 1;
        }
        if (FoottrollerCtrl.instance.btnB)
        {
            tempval = tempval + 2;
        }
        if (FoottrollerCtrl.instance.btnX)
        {
            tempval = tempval + 4;
        }
        if (FoottrollerCtrl.instance.btnY)
        {
            tempval = tempval + 8;
        }
        if (FoottrollerCtrl.instance.triggerL)         // left triger value
        {
            tempval = tempval + 16;
        }
        if (FoottrollerCtrl.instance.triggerR)          // right trigger value
        {
            tempval = tempval + 32;
        }else if (tempval < 0) {
            tempval = 0;
        }
        Byte2Data = (byte)tempval;
        byte Byte3Data;
        tempval = (int)Mathf.Floor(FoottrollerCtrl.instance.triggerL_value * 255);
        if (tempval > 255)
        {
            tempval = 255;
        }
        else if (tempval < 0)
        {
            tempval = 0;
        }
        Byte3Data =  (byte)tempval;
        // byte Byte4Data;
        tempval = (int)Mathf.Floor(FoottrollerCtrl.instance.triggerR_value * 255);
        if (tempval > 255)
        {
            tempval = 255;
        }
        else if (tempval < 0)
        {
            tempval = 0;
        }
        Byte4Data = (byte)tempval;
        byte Byte5Data;
        tempval = (int)Mathf.Floor(FoottrollerCtrl.instance.joystick_RH.x * 128 + 128f);
        if (tempval > 255)
        {
            tempval = 255;
        }
        else if (tempval < 0)
        {
            tempval = 0;
        }
        Byte5Data = (byte)tempval;
        byte Byte6Data;
        tempval = (int)Mathf.Floor(FoottrollerCtrl.instance.joystick_RH.y * 128 + 128f);
        if (tempval > 255)
        {
            tempval = 255;
        }
        else if (tempval < 0)
        {
            tempval = 0;
        }
        Byte6Data = (byte)tempval;
        byte Byte7Data;
        tempval = (int)Mathf.Floor(FoottrollerCtrl.instance.joystick_LH.x * 128 + 128f);
        if (tempval > 255) { 
            tempval = 255;
        }else if (tempval < 0) {
            tempval = 0;
        }
        Byte7Data = (byte)tempval;
        byte Byte8Data;
        tempval = (int)Mathf.Floor(FoottrollerCtrl.instance.joystick_LH.y * 128 + 128f);
        if (tempval > 255)
        {
            tempval = 255;
        }
        else if (tempval < 0)
        {
            tempval = 0;
        }
        Byte8Data = (byte)tempval;

        testflag = false;

        if (pre_Byte1Data - Byte1Data > 3 || pre_Byte1Data - Byte1Data < -3) {
            testflag = true;
        }
        if (pre_Byte2Data != Byte2Data)
        {
            testflag = true;
        }
        if (pre_Byte3Data - Byte3Data > 3 || pre_Byte3Data - Byte3Data < -3)
        {
            testflag = true;
        }
        if (pre_Byte4Data - Byte4Data > 3 || pre_Byte4Data - Byte4Data < -3)
        {
            testflag = true;
        }
        if (pre_Byte5Data - Byte5Data > 5 || pre_Byte5Data - Byte5Data < -5)
        {
            testflag = true;
        }
        if (pre_Byte6Data - Byte6Data > 5 || pre_Byte6Data - Byte6Data < -5)
        {
            testflag = true;
        }
        if (pre_Byte7Data - Byte7Data > 5 || pre_Byte7Data - Byte7Data < -5)
        {
            testflag = true;
        }
        if (pre_Byte8Data - Byte8Data > 5 || pre_Byte8Data - Byte8Data < -5)
        {
            testflag = true;
        }

        if (udpConnReady && !udpReconnflag && testflag) {
            IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(serverip), 45041);
            udpBufsend[0] = 32;            // remote dev to Foottroller server
            udpBufsend[1] = Byte1Data;
            udpBufsend[2] = Byte2Data;
            udpBufsend[3] = Byte3Data;    // left trigger
            udpBufsend[4] = Byte4Data;    // right trigger
            udpBufsend[5] = Byte5Data;
            udpBufsend[6] = Byte6Data;
            udpBufsend[7] = Byte7Data;
            udpBufsend[8] = Byte8Data;

            pre_Byte1Data = Byte1Data;
            pre_Byte2Data = Byte2Data;
            pre_Byte3Data = Byte3Data;
            pre_Byte4Data = Byte4Data;
            pre_Byte5Data = Byte5Data;
            pre_Byte6Data = Byte6Data;
            pre_Byte7Data = Byte7Data;
            pre_Byte8Data = Byte8Data;
            try
            {
                client_S.Send(udpBufsend, 9, serverEndPoint);
            }
            catch (Exception err)
            {
            }

        }

    }
    // init
    private void init()
    {
        // Endpunkt definieren, von dem die Nachrichten gesendet werden.
        //print("UDPSend.init()");

        // define port
        port = 8051;

        // status
        //print("Sending to 127.0.0.1 : " + port);
        //print("Test-Sending to this Port: nc -u 127.0.0.1  " + port + "");


        // ----------------------------
        // Abhören
        // ----------------------------
        // Lokalen Endpunkt definieren (wo Nachrichten empfangen werden).
        // Einen neuen Thread für den Empfang eingehender Nachrichten erstellen.

        //client = new UdpClient(senderport + 1);
        // https://stackoverflow.com/questions/25243781/udp-sending-receiving-on-a-free-port
        // create a udp listener at a random port and get the port number
        // Sending on a port determined by the linstener port
        IPEndPoint localEndpoint = new IPEndPoint(IPAddress.Any, 0);
        // listenport = 15000;
        // client = new UdpClient(listenport);
        client = new UdpClient();
        client.Client.Bind(localEndpoint);
        listenport = ((IPEndPoint)(client.Client.LocalEndPoint)).Port;
        senderport = listenport-1;

        receiveThread = new Thread(
            new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();

    }

    // receive thread
    private void ReceiveData()
    {

        //client = new UdpClient(port);

        // client = new UdpClient(45040);


        while (true)
        {
            //Debug.Log("In udp loop");
            try
            {
                // Bytes empfangen.
                IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = client.Receive(ref anyIP);
                // udpConnReady = true;
                // process connection message
                // Debug.Log("In udp loop: data received");
                if (!udpConnReady && data.Length == 2) {
                    if (data[0] == 1)
                    {
                        if (data[1] == 2)
                        {
                            server_data_sink_ready = 1;
                            udpConnReady = true;
                            serverip = anyIP.Address.ToString(); // update server ip
                        }
                    }
                    continue;
                }

                if (udpConnReady && data.Length == 8) {
                    if (data[0] == 31) { // full foottroller data trans
                        lastupdatems = 0;
                        joystick_x = (data[1] - 128) / 128.0f;
                        joystick_y = (data[2] - 128) / 128.0f;
                        RFheading = data[3] * 360.0f / 255.0f;
                        RFtilt = (data[4] - 128);
                        LFheading = data[5] * 360.0f / 255.0f;
                        LFtilt = (data[6] - 128);
                        TSRF = data[7] & 0x0C;
                        TSRF = TSRF / 4;
                        TSLF = data[7] & 0x03;
                    }
                    udpReconnflag = false;
                }
                
                /*
                if (true)
                {

                    if (true)
                    {
                        serverip = anyIP.Address.ToString(); // update server ip
                        udpConnReady = true;
                    }
                    lastupdatems = 0;
                }*/

                // Bytes mit der UTF8-Kodierung in das Textformat kodieren.
                //string text = Encoding.UTF8.GetString(data);

                // Den abgerufenen Text anzeigen.
                // print(">> " + text);

                // latest UDPpacket
                //lastReceivedUDPPacket = text;

                // ....
                //allReceivedUDPPackets = allReceivedUDPPackets + text;
            }
            catch (Exception err)
            {
                //print(err.ToString());
                reinitudpflag = true;
            }

        }
    }

    int anglediffeval(int angle) {
        int angle1 = 360 + angle;
        int angle2 = angle - 360;
        if (Mathf.Abs(angle1) < Mathf.Abs(angle2) && Mathf.Abs(angle1) < Mathf.Abs(angle))
        {
            return angle1;
        }
        else if (Mathf.Abs(angle2) < Mathf.Abs(angle1) && Mathf.Abs(angle2) < Mathf.Abs(angle))
        {
            return angle2;
        }
        else {
            return angle;
        }
    }
    public void init_S()
    {
        // Endpunkt definieren, von dem die Nachrichten gesendet werden.
        // abcprint("UDPSend.init()");

        // define
        IP_S = "192.168.1.255";
        IP_S = localIPs;
        port_S = 45041; //  12309;

        // ----------------------------
        // Senden
        // ----------------------------
        remoteEndPoint = new IPEndPoint(IPAddress.Parse(IP_S), port_S);
        broadcastEndPoint = new IPEndPoint(IPAddress.Parse(IP_S), port_S);
        //client_S = new UdpClient();
        
        client_S = new UdpClient(senderport);
        

        //sendString("udp test");
        udpBufsend = new Byte[50];
        udpData_len = 0;
        //Debug.Log("Net ini");
        // senderport = remoteEndPoint.Port;

    }

    private void reinit() {

        IPEndPoint localEndpoint = new IPEndPoint(IPAddress.Any, 0);
        if (client != null) {
            client.Close();
        }
        client = new UdpClient();
        client.Client.Bind(localEndpoint);
        listenport = ((IPEndPoint)(client.Client.LocalEndPoint)).Port;
        senderport = listenport - 1;


        if (client_S != null) {
            client_S.Close();
        }
        client_S = new UdpClient(senderport);
    }
    // sendData
    private void sendString(string message)
    {
        try
        {
            //if (message != "")
            //{

            // Daten mit der UTF8-Kodierung in das Binärformat kodieren.
            byte[] data = Encoding.UTF8.GetBytes(message);

            // Den message zum Remote-Client senden.
            client_S.Send(data, data.Length, remoteEndPoint);
            //}
        }
        catch (Exception err)
        {
            // print(err.ToString());
        }
    }
    private void sendudpData()
    {
        try
        {
            //if (message != "")
            //{

            // Daten mit der UTF8-Kodierung in das Binärformat kodieren.
            // Den message zum Remote-Client senden.
            client_S.Send(udpBufsend, udpData_len, remoteEndPoint);
            //}
            reinitcnt = 0;
        }
        catch (Exception err)
        {
            // print(err.ToString());
            reinitudpflag = true;
            reinitcnt = reinitcnt + 1;
        }
    }


    string LocalIPAddress() {
        IPHostEntry host;
        string localIP = "0.0.0.0";

        localIP = "192.168.0.129";   // oculus quest 2 cannot send broadcast msg need to send to single ip to establish conn
        //return localIP;

        host = Dns.GetHostEntry(Dns.GetHostName());
        int ipcnt = 0;
        foreach (IPAddress ip in host.AddressList) {
            if (ip.AddressFamily == AddressFamily.InterNetwork) {
                ipcnt = ipcnt + 1;
                if (ipcnt == ipdix + 1) {
                    localIP = ip.ToString();
                    localIP = localIP.Substring(0, localIP.LastIndexOf(".") + 1);
                    //localIP += "255";
                    break;
                }
                //localIP += ip.ToString();
            }
        }
        ipdix = ipdix + 1;
        if (ipdix == ipcnt) {
            ipdix = 0;
        }
        //localIP = "192.168.0.255";
        return localIP;

    }
}
