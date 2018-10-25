using System;
using System.Text;

namespace RtpStreamCapture
{
  /// <summary>
  /// https://github.com/pruiz/LumiSoft.Net/blob/master/Net/RTP/RTP_Packet.cs
  /// A data packet consisting of the fixed RTP header, a possibly empty list of contributing
  /// sources (see below), and the payload data. Some underlying protocols may require an
  /// encapsulation of the RTP packet to be defined. Typically one packet of the underlying
  /// protocol contains a single RTP packet, but several RTP packets MAY be contained if
  /// permitted by the encapsulation method (see Section 11).
  /// </summary>
  public class RtpPacket
  {
    private int m_PayloadType = 0;
    private uint m_Timestamp = 0;
    private uint m_SSRC = 0;
    private byte[] m_Data = null;

    /// <summary>
    /// Default constructor.
    /// </summary>
    public RtpPacket()
    {
    }

    #region static method Parse

    /// <summary>
    /// Parses RTP packet.
    /// </summary>
    /// <param name="buffer">Buffer containing RTP packet.</param>
    /// <param name="size">Number of bytes used in buffer.</param>
    /// <returns>Returns parsed RTP packet.</returns>
    public static RtpPacket Parse(byte[] buffer, int size)
    {
      RtpPacket packet = new RtpPacket();
      packet.ParseInternal(buffer, size);

      return packet;
    }

    #endregion static method Parse

    #region method Validate

    /// <summary>
    /// Validates RTP packet.
    /// </summary>
    public void Validate()
    {
      // TODO: Validate RTP apcket
    }

    #endregion method Validate

    #region method ToByte

    /// <summary>
    /// Stores this packet to the specified buffer.
    /// </summary>
    /// <param name="buffer">Buffer where to store packet.</param>
    /// <param name="offset">Offset in buffer.</param>
    public void ToByte(byte[] buffer, ref int offset)
    {
      /* RFC 3550.5.1 RTP Fixed Header Fields.

          The RTP header has the following format:

          0                   1                   2                   3
          0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
         +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
         |V=2|P|X|  CC   |M|     PT      |       sequence number         |
         +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
         |                           timestamp                           |
         +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
         |           synchronization source (SSRC) identifier            |
         +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
         |            contributing source (CSRC) identifiers             |
         |                             ....                              |
         +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

         5.3. Available if X bit filled.
          0                   1                   2                   3
          0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
         +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
         |      defined by profile       |           length              |
         +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
         |                        header extension                       |
         |                             ....                              |

      */

      int cc = 0;
      if (CSRC != null)
      {
        cc = CSRC.Length;
      }

      // V P X CC
      buffer[offset++] = (byte)(Version << 6 | 0 << 5 | cc & 0xF);
      // M PT
      buffer[offset++] = (byte)(Convert.ToInt32(IsMarker) << 7 | m_PayloadType & 0x7F);
      // sequence number
      buffer[offset++] = (byte)(SeqNo >> 8);
      buffer[offset++] = (byte)(SeqNo & 0xFF);
      // timestamp
      buffer[offset++] = (byte)((m_Timestamp >> 24) & 0xFF);
      buffer[offset++] = (byte)((m_Timestamp >> 16) & 0xFF);
      buffer[offset++] = (byte)((m_Timestamp >> 8) & 0xFF);
      buffer[offset++] = (byte)(m_Timestamp & 0xFF);
      // SSRC
      buffer[offset++] = (byte)((m_SSRC >> 24) & 0xFF);
      buffer[offset++] = (byte)((m_SSRC >> 16) & 0xFF);
      buffer[offset++] = (byte)((m_SSRC >> 8) & 0xFF);
      buffer[offset++] = (byte)(m_SSRC & 0xFF);
      // CSRCs
      if (CSRC != null)
      {
        foreach (int csrc in CSRC)
        {
          buffer[offset++] = (byte)((csrc >> 24) & 0xFF);
          buffer[offset++] = (byte)((csrc >> 16) & 0xFF);
          buffer[offset++] = (byte)((csrc >> 8) & 0xFF);
          buffer[offset++] = (byte)(csrc & 0xFF);
        }
      }
      // X
      Array.Copy(m_Data, 0, buffer, offset, m_Data.Length);
      offset += m_Data.Length;
    }

    #endregion method ToByte

    #region override method ToString

    /// <summary>
    /// Returns this packet info as string.
    /// </summary>
    /// <returns>Returns packet info.</returns>
    public override string ToString()
    {
      StringBuilder retVal = new StringBuilder();
      retVal.Append("----- RTP Packet\r\n");
      retVal.Append("Version: " + Version.ToString() + "\r\n");
      retVal.Append("IsMaker: " + IsMarker.ToString() + "\r\n");
      retVal.Append("PayloadType: " + m_PayloadType.ToString() + "\r\n");
      retVal.Append("SeqNo: " + SeqNo.ToString() + "\r\n");
      retVal.Append("Timestamp: " + m_Timestamp.ToString() + "\r\n");
      retVal.Append("SSRC: " + m_SSRC.ToString() + "\r\n");
      retVal.Append("Data: " + m_Data.Length + " bytes.\r\n");

      return retVal.ToString();
    }

    #endregion override method ToString

    #region method ParseInternal

    /// <summary>
    /// Parses RTP packet from the specified buffer.
    /// </summary>
    /// <param name="buffer">Buffer containing RTP packet.</param>
    /// <param name="size">Number of bytes used in buffer.</param>
    private void ParseInternal(byte[] buffer, int size)
    {
      /* RFC 3550.5.1 RTP Fixed Header Fields.

          The RTP header has the following format:

          0                   1                   2                   3
          0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
         +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
         |V=2|P|X|  CC   |M|     PT      |       sequence number         |
         +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
         |                           timestamp                           |
         +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
         |           synchronization source (SSRC) identifier            |
         +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
         |            contributing source (CSRC) identifiers             |
         |                             ....                              |
         +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

         5.3. Available if X bit filled.
          0                   1                   2                   3
          0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
         +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
         |      defined by profile       |           length              |
         +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
         |                        header extension                       |
         |                             ....                              |

      */

      int offset = 0;

      // V
      Version = buffer[offset] >> 6;
      // P
      bool isPadded = Convert.ToBoolean((buffer[offset] >> 5) & 0x1);
      // X
      bool hasExtention = Convert.ToBoolean((buffer[offset] >> 4) & 0x1);
      // CC
      int csrcCount = buffer[offset++] & 0xF;
      // M
      IsMarker = Convert.ToBoolean(buffer[offset] >> 7);
      // PT
      m_PayloadType = buffer[offset++] & 0x7F;
      // sequence number
      SeqNo = (ushort)(buffer[offset++] << 8 | buffer[offset++]);
      // timestamp
      m_Timestamp = (uint)(buffer[offset++] << 24 | buffer[offset++] << 16 | buffer[offset++] << 8 | buffer[offset++]);
      // SSRC
      m_SSRC = (uint)(buffer[offset++] << 24 | buffer[offset++] << 16 | buffer[offset++] << 8 | buffer[offset++]);
      // CSRC
      CSRC = new uint[csrcCount];
      for (int i = 0; i < csrcCount; i++)
      {
        CSRC[i] = (uint)(buffer[offset++] << 24 | buffer[offset++] << 16 | buffer[offset++] << 8 | buffer[offset++]);
      }
      // X
      if (hasExtention)
      {
        // Skip extention
        offset++;
        offset += buffer[offset];
      }

      // TODO: Padding

      // Data
      m_Data = new byte[size - offset];
      Array.Copy(buffer, offset, m_Data, 0, m_Data.Length);
    }

    #endregion method ParseInternal

    #region Properties Implementation

    /// <summary>
    /// Gets RTP version.
    /// </summary>
    public int Version { get; private set; } = 2;

    /// <summary>
    /// Gets if packet is padded to some bytes boundary.
    /// </summary>
    public bool IsPadded
    {
      get { return false; }
    }

    /// <summary>
    /// Gets marker bit. The usage of this bit depends on payload type.
    /// </summary>
    public bool IsMarker { get; set; } = false;

    /// <summary>
    /// Gets payload type.
    /// </summary>
    /// <exception cref="ArgumentException">Is raised when invalid value is passed.</exception>
    public int PayloadType
    {
      get { return m_PayloadType; }

      set
      {
        if (value < 0 || value > 128)
        {
          throw new ArgumentException("Payload value must be >= 0 and <= 128.");
        }

        m_PayloadType = value;
      }
    }

    /// <summary>
    /// Gets or sets RTP packet sequence number.
    /// </summary>
    /// <exception cref="ArgumentException">Is raised when invalid value is passed.</exception>
    public ushort SeqNo { get; set; } = 0;

    /// <summary>
    /// Gets sets packet timestamp.
    /// </summary>
    /// <exception cref="ArgumentException">Is raised when invalid value is passed.</exception>
    public uint Timestamp
    {
      get { return m_Timestamp; }

      set
      {
        if (value < 1)
        {
          throw new ArgumentException("Timestamp value must be >= 1.");
        }

        m_Timestamp = value;
      }
    }

    /// <summary>
    /// Gets or sets synchronization source ID.
    /// </summary>
    /// <exception cref="ArgumentException">Is raised when invalid value is passed.</exception>
    public uint SSRC
    {
      get { return m_SSRC; }

      set
      {
        if (value < 1)
        {
          throw new ArgumentException("SSRC value must be >= 1.");
        }

        m_SSRC = value;
      }
    }

    /// <summary>
    /// Gets or sets the contributing sources for the payload contained in this packet.
    /// Value null means none.
    /// </summary>
    public uint[] CSRC { get; set; } = null;

    /// <summary>
    /// Gets SSRC + CSRCs as joined array.
    /// </summary>
    public uint[] Sources
    {
      get
      {
        uint[] retVal = new uint[1];
        if (CSRC != null)
        {
          retVal = new uint[1 + CSRC.Length];
        }
        retVal[0] = m_SSRC;
        Array.Copy(CSRC, retVal, CSRC.Length);

        return retVal;
      }
    }

    /// <summary>
    /// Gets or sets RTP data. Data must be encoded with PayloadType encoding.
    /// </summary>
    /// <exception cref="ArgumentNullException">Is raised when null value is passed.</exception>
    public byte[] Data
    {
      get { return m_Data; }

      set
      {
        m_Data = value ?? throw new ArgumentNullException("Data");
      }
    }

    #endregion Properties Implementation
  }
}
