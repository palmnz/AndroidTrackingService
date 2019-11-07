using System;
using System.Text;

namespace CANLib
{
	public static class HexDump
	{
		public static string Dump(byte[] buffer)
		{
			return Dump(buffer, buffer.Length);
		}

		public static string Dump(byte[] buffer, int count)
		{
			int rowSize = 8;
			StringBuilder dump = new StringBuilder();

			for (int i = 0; i < count; i += rowSize)
			{
				int c;
				for (c = 0; c < rowSize && ((i + c) < count); ++c)
					dump.AppendFormat("{0:X2} ", buffer[i + c]);
				for ( ; c < rowSize + 1; ++c)
					dump.Append("   ");
				for (c = 0; c < rowSize && ((i + c) < count); ++c)
				{
					if (buffer[i + c] < 32 || buffer[i + c] > 127)
						dump.Append(". ");
					else
						dump.AppendFormat("{0} ", Encoding.ASCII.GetString(buffer, i + c, 1));
				}
				dump.Append(Environment.NewLine);
			}
			return dump.ToString();
		}
	}
}
