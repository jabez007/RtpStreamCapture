using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using PacketDotNet;
using SharpPcap;
using System;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RtpStreamCapture
{
  class Program
  {
    private static int port = 10000;
  
    /// <summary>
    /// https://tools.ietf.org/html/rfc3551#page-32
    /// encodingName: PCMU
    /// mediaType: Audio
    /// clockRate: 8,000 Hz
    /// channels: 1
    /// </summary>
    private static MuLawChatCodec codec = new MuLawChatCodec();
    private static BufferedWaveProvider incomingWaveProvider = new BufferedWaveProvider(codec.RecordFormat);
    private static BufferedWaveProvider outgoingWaveProvider = new BufferedWaveProvider(codec.RecordFormat);
    private static WaveFileWriter recordIncoming = new WaveFileWriter("testIncoming.wav", codec.RecordFormat);
    private static WaveFileWriter recordOutgoing = new WaveFileWriter("testOutgoing.wav", codec.RecordFormat);
    
    static void Main(string[] args)
    {
      foreach (var d in CaptureDeviceList.Instance)
      {
        d.OnPacketArrival += OnPacketArrival;
        d.Open();
        d.Filter = string.Format("port {0}", port);
        d.StartCapture();
      }
      
      Console.WriteLine("Press Enter to stop...");
      Console.ReadLine();
      
      recordIncoming.Dispose();
      recordOutgoing.Dispose();
    }
    
    private static void OnPacketArrival(object sender, CaptureEventArgs e)
    {
      var packet = Packet.ParsePacket(e.Packet.LinkLayerType, e.Packet.Data);
      var udpPacket = (UdpPacket)packet.Extract(typeof(UdpPacket));
      var payload = udpPacket.PayloadData;
      var rtp = RtpPacket.Parse(payload, payload.Length);
      byte[] decodedAudio = codec.Decode(rtp.Data, 0, rtp.Data.Length);
      if (udpPacket.DestinationPort == port)
      {
        recordIncoming.Write(decodedAudio, 0, decodedAudio.Length);
      }
      else
      {
        recordOutgoing.Write(decodedAudio, 0, decodedAudio.Length);
      } 
    }
  }
}
