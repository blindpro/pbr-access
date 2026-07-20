using System.IO;
using System.Text;

namespace SimpleJSON;

public class JSONNull : JSONNode
{
	public override JSONNodeType Tag => JSONNodeType.NullValue;

	public override bool IsNull => true;

	public override string Value
	{
		get
		{
			return "null";
		}
		set
		{
		}
	}

	public override bool AsBool
	{
		get
		{
			return false;
		}
		set
		{
		}
	}

	public override bool Equals(object obj)
	{
		if ((object)this == obj)
		{
			return true;
		}
		return obj is JSONNull;
	}

	public override int GetHashCode()
	{
		return 0;
	}

	public override void Serialize(BinaryWriter aWriter)
	{
		aWriter.Write((byte)5);
	}

	internal override void WriteToStringBuilder(StringBuilder aSB, int aIndent, int aIndentInc, JSONTextMode aMode)
	{
		aSB.Append("null");
	}
}
