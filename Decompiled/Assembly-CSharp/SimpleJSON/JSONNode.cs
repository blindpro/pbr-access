using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace SimpleJSON;

public abstract class JSONNode
{
	internal static StringBuilder m_EscapeBuilder = new StringBuilder();

	public virtual JSONNode this[int aIndex]
	{
		get
		{
			return null;
		}
		set
		{
		}
	}

	public virtual JSONNode this[string aKey]
	{
		get
		{
			return null;
		}
		set
		{
		}
	}

	public virtual string Value
	{
		get
		{
			return "";
		}
		set
		{
		}
	}

	public virtual int Count => 0;

	public virtual bool IsNumber => false;

	public virtual bool IsString => false;

	public virtual bool IsBoolean => false;

	public virtual bool IsNull => false;

	public virtual bool IsArray => false;

	public virtual bool IsObject => false;

	public virtual IEnumerable<JSONNode> Children
	{
		get
		{
			yield break;
		}
	}

	public IEnumerable<JSONNode> DeepChildren
	{
		get
		{
			foreach (JSONNode child in Children)
			{
				foreach (JSONNode deepChild in child.DeepChildren)
				{
					yield return deepChild;
				}
			}
		}
	}

	public abstract JSONNodeType Tag { get; }

	public virtual double AsDouble
	{
		get
		{
			double result = 0.0;
			if (double.TryParse(Value, out result))
			{
				return result;
			}
			return 0.0;
		}
		set
		{
			Value = value.ToString();
		}
	}

	public virtual int AsInt
	{
		get
		{
			return (int)AsDouble;
		}
		set
		{
			AsDouble = value;
		}
	}

	public virtual float AsFloat
	{
		get
		{
			return (float)AsDouble;
		}
		set
		{
			AsDouble = value;
		}
	}

	public virtual bool AsBool
	{
		get
		{
			bool result = false;
			if (bool.TryParse(Value, out result))
			{
				return result;
			}
			return !string.IsNullOrEmpty(Value);
		}
		set
		{
			Value = (value ? "true" : "false");
		}
	}

	public virtual JSONArray AsArray => this as JSONArray;

	public virtual JSONObject AsObject => this as JSONObject;

	public virtual void Add(string aKey, JSONNode aItem)
	{
	}

	public virtual void Add(JSONNode aItem)
	{
		Add("", aItem);
	}

	public virtual JSONNode Remove(string aKey)
	{
		return null;
	}

	public virtual JSONNode Remove(int aIndex)
	{
		return null;
	}

	public virtual JSONNode Remove(JSONNode aNode)
	{
		return aNode;
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		WriteToStringBuilder(stringBuilder, 0, 0, JSONTextMode.Compact);
		return stringBuilder.ToString();
	}

	public virtual string ToString(int aIndent)
	{
		StringBuilder stringBuilder = new StringBuilder();
		WriteToStringBuilder(stringBuilder, 0, aIndent, JSONTextMode.Indent);
		return stringBuilder.ToString();
	}

	internal abstract void WriteToStringBuilder(StringBuilder aSB, int aIndent, int aIndentInc, JSONTextMode aMode);

	public static implicit operator JSONNode(string s)
	{
		return new JSONString(s);
	}

	public static implicit operator string(JSONNode d)
	{
		if (!(d == null))
		{
			return d.Value;
		}
		return null;
	}

	public static implicit operator JSONNode(double n)
	{
		return new JSONNumber(n);
	}

	public static implicit operator double(JSONNode d)
	{
		if (!(d == null))
		{
			return d.AsDouble;
		}
		return 0.0;
	}

	public static implicit operator JSONNode(float n)
	{
		return new JSONNumber(n);
	}

	public static implicit operator float(JSONNode d)
	{
		if (!(d == null))
		{
			return d.AsFloat;
		}
		return 0f;
	}

	public static implicit operator JSONNode(int n)
	{
		return new JSONNumber(n);
	}

	public static implicit operator int(JSONNode d)
	{
		if (!(d == null))
		{
			return d.AsInt;
		}
		return 0;
	}

	public static implicit operator JSONNode(bool b)
	{
		return new JSONBool(b);
	}

	public static implicit operator bool(JSONNode d)
	{
		if (!(d == null))
		{
			return d.AsBool;
		}
		return false;
	}

	public static bool operator ==(JSONNode a, object b)
	{
		if ((object)a == b)
		{
			return true;
		}
		bool num = a is JSONNull || (object)a == null || a is JSONLazyCreator;
		bool flag = b is JSONNull || b == null || b is JSONLazyCreator;
		if (num && flag)
		{
			return true;
		}
		return a.Equals(b);
	}

	public static bool operator !=(JSONNode a, object b)
	{
		return !(a == b);
	}

	public override bool Equals(object obj)
	{
		return (object)this == obj;
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	internal static string Escape(string aText)
	{
		m_EscapeBuilder.Length = 0;
		if (m_EscapeBuilder.Capacity < aText.Length + aText.Length / 10)
		{
			m_EscapeBuilder.Capacity = aText.Length + aText.Length / 10;
		}
		foreach (char c in aText)
		{
			switch (c)
			{
			case '\\':
				m_EscapeBuilder.Append("\\\\");
				break;
			case '"':
				m_EscapeBuilder.Append("\\\"");
				break;
			case '\n':
				m_EscapeBuilder.Append("\\n");
				break;
			case '\r':
				m_EscapeBuilder.Append("\\r");
				break;
			case '\t':
				m_EscapeBuilder.Append("\\t");
				break;
			case '\b':
				m_EscapeBuilder.Append("\\b");
				break;
			case '\f':
				m_EscapeBuilder.Append("\\f");
				break;
			default:
				m_EscapeBuilder.Append(c);
				break;
			}
		}
		string result = m_EscapeBuilder.ToString();
		m_EscapeBuilder.Length = 0;
		return result;
	}

	private static void ParseElement(JSONNode ctx, string token, string tokenName, bool quoted)
	{
		if (quoted)
		{
			ctx.Add(tokenName, token);
			return;
		}
		string text = token.ToLower();
		switch (text)
		{
		case "false":
		case "true":
			ctx.Add(tokenName, text == "true");
			return;
		case "null":
			ctx.Add(tokenName, null);
			return;
		}
		if (double.TryParse(token, out var result))
		{
			ctx.Add(tokenName, result);
		}
		else
		{
			ctx.Add(tokenName, token);
		}
	}

	public static JSONNode Parse(string aJSON)
	{
		Stack<JSONNode> stack = new Stack<JSONNode>();
		JSONNode jSONNode = null;
		int i = 0;
		StringBuilder stringBuilder = new StringBuilder();
		string text = "";
		bool flag = false;
		bool flag2 = false;
		for (; i < aJSON.Length; i++)
		{
			switch (aJSON[i])
			{
			case '{':
				if (flag)
				{
					stringBuilder.Append(aJSON[i]);
					break;
				}
				stack.Push(new JSONObject());
				if (jSONNode != null)
				{
					jSONNode.Add(text, stack.Peek());
				}
				text = "";
				stringBuilder.Length = 0;
				jSONNode = stack.Peek();
				break;
			case '[':
				if (flag)
				{
					stringBuilder.Append(aJSON[i]);
					break;
				}
				stack.Push(new JSONArray());
				if (jSONNode != null)
				{
					jSONNode.Add(text, stack.Peek());
				}
				text = "";
				stringBuilder.Length = 0;
				jSONNode = stack.Peek();
				break;
			case ']':
			case '}':
				if (flag)
				{
					stringBuilder.Append(aJSON[i]);
					break;
				}
				if (stack.Count == 0)
				{
					throw new Exception("JSON Parse: Too many closing brackets");
				}
				stack.Pop();
				if (stringBuilder.Length > 0 || flag2)
				{
					ParseElement(jSONNode, stringBuilder.ToString(), text, flag2);
					flag2 = false;
				}
				text = "";
				stringBuilder.Length = 0;
				if (stack.Count > 0)
				{
					jSONNode = stack.Peek();
				}
				break;
			case ':':
				if (flag)
				{
					stringBuilder.Append(aJSON[i]);
					break;
				}
				text = stringBuilder.ToString();
				stringBuilder.Length = 0;
				flag2 = false;
				break;
			case '"':
				flag = !flag;
				flag2 = flag2 || flag;
				break;
			case ',':
				if (flag)
				{
					stringBuilder.Append(aJSON[i]);
					break;
				}
				if (stringBuilder.Length > 0 || flag2)
				{
					ParseElement(jSONNode, stringBuilder.ToString(), text, flag2);
					flag2 = false;
				}
				text = "";
				stringBuilder.Length = 0;
				flag2 = false;
				break;
			case '\t':
			case ' ':
				if (flag)
				{
					stringBuilder.Append(aJSON[i]);
				}
				break;
			case '\\':
				i++;
				if (flag)
				{
					char c = aJSON[i];
					switch (c)
					{
					case 't':
						stringBuilder.Append('\t');
						break;
					case 'r':
						stringBuilder.Append('\r');
						break;
					case 'n':
						stringBuilder.Append('\n');
						break;
					case 'b':
						stringBuilder.Append('\b');
						break;
					case 'f':
						stringBuilder.Append('\f');
						break;
					case 'u':
					{
						string s = aJSON.Substring(i + 1, 4);
						stringBuilder.Append((char)int.Parse(s, NumberStyles.AllowHexSpecifier));
						i += 4;
						break;
					}
					default:
						stringBuilder.Append(c);
						break;
					}
				}
				break;
			default:
				stringBuilder.Append(aJSON[i]);
				break;
			case '\n':
			case '\r':
				break;
			}
		}
		if (flag)
		{
			throw new Exception("JSON Parse: Quotation marks seems to be messed up.");
		}
		return jSONNode;
	}

	public static JSONNode ParseEx(string aJSON)
	{
		Stack<JSONNode> stack = new Stack<JSONNode>();
		JSONNode jSONNode = null;
		int i = 0;
		StringBuilder stringBuilder = new StringBuilder();
		string text = "";
		bool flag = false;
		bool flag2 = false;
		for (; i < aJSON.Length; i++)
		{
			switch (aJSON[i])
			{
			case '{':
				if (flag)
				{
					stringBuilder.Append(aJSON[i]);
					break;
				}
				stack.Push(new JSONObject());
				if (jSONNode != null)
				{
					jSONNode.Add(text, stack.Peek());
				}
				text = "";
				stringBuilder.Length = 0;
				jSONNode = stack.Peek();
				break;
			case '[':
				if (flag)
				{
					stringBuilder.Append(aJSON[i]);
					break;
				}
				stack.Push(new JSONArray());
				if (jSONNode != null)
				{
					jSONNode.Add(text, stack.Peek());
				}
				text = "";
				stringBuilder.Length = 0;
				jSONNode = stack.Peek();
				break;
			case ']':
			case '}':
				if (flag)
				{
					stringBuilder.Append(aJSON[i]);
					break;
				}
				if (stack.Count == 0)
				{
					throw new Exception("JSON Parse: Too many closing brackets");
				}
				stack.Pop();
				if (stringBuilder.Length > 0 || flag2)
				{
					ParseElement(jSONNode, stringBuilder.ToString(), text, flag2);
					flag2 = false;
				}
				text = "";
				stringBuilder.Length = 0;
				if (stack.Count > 0)
				{
					jSONNode = stack.Peek();
				}
				break;
			case ':':
				if (flag)
				{
					stringBuilder.Append(aJSON[i]);
					break;
				}
				text = stringBuilder.ToString();
				stringBuilder.Length = 0;
				flag2 = false;
				break;
			case '"':
				flag = !flag;
				flag2 = flag2 || flag;
				break;
			case ',':
				if (flag)
				{
					stringBuilder.Append(aJSON[i]);
					break;
				}
				if (stringBuilder.Length > 0 || flag2)
				{
					ParseElement(jSONNode, stringBuilder.ToString(), text, flag2);
					flag2 = false;
				}
				text = "";
				stringBuilder.Length = 0;
				flag2 = false;
				break;
			case '\t':
			case '\r':
			case ' ':
				if (flag)
				{
					stringBuilder.Append(aJSON[i]);
				}
				break;
			case '\\':
				i++;
				if (flag)
				{
					char c = aJSON[i];
					switch (c)
					{
					case 't':
						stringBuilder.Append('\t');
						break;
					case 'r':
						stringBuilder.Append('\r');
						break;
					case 'n':
						stringBuilder.Append('\n');
						break;
					case 'b':
						stringBuilder.Append('\b');
						break;
					case 'f':
						stringBuilder.Append('\f');
						break;
					case 'u':
					{
						string s = aJSON.Substring(i + 1, 4);
						stringBuilder.Append((char)int.Parse(s, NumberStyles.AllowHexSpecifier));
						i += 4;
						break;
					}
					default:
						stringBuilder.Append(c);
						break;
					}
				}
				break;
			default:
				stringBuilder.Append(aJSON[i]);
				break;
			}
		}
		if (flag)
		{
			throw new Exception("JSON Parse: Quotation marks seems to be messed up.");
		}
		return jSONNode;
	}

	public virtual void Serialize(BinaryWriter aWriter)
	{
	}

	public void SaveToStream(Stream aData)
	{
		BinaryWriter aWriter = new BinaryWriter(aData);
		Serialize(aWriter);
	}

	public void SaveToCompressedStream(Stream aData)
	{
		throw new Exception("Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
	}

	public void SaveToCompressedFile(string aFileName)
	{
		throw new Exception("Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
	}

	public string SaveToCompressedBase64()
	{
		throw new Exception("Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
	}

	public void SaveToFile(string aFileName)
	{
		Directory.CreateDirectory(new FileInfo(aFileName).Directory.FullName);
		using FileStream aData = File.OpenWrite(aFileName);
		SaveToStream(aData);
	}

	public string SaveToBase64()
	{
		using MemoryStream memoryStream = new MemoryStream();
		SaveToStream(memoryStream);
		memoryStream.Position = 0L;
		return Convert.ToBase64String(memoryStream.ToArray());
	}

	public static JSONNode Deserialize(BinaryReader aReader)
	{
		JSONNodeType jSONNodeType = (JSONNodeType)aReader.ReadByte();
		switch (jSONNodeType)
		{
		case JSONNodeType.Array:
		{
			int num2 = aReader.ReadInt32();
			JSONArray jSONArray = new JSONArray();
			for (int j = 0; j < num2; j++)
			{
				jSONArray.Add(Deserialize(aReader));
			}
			return jSONArray;
		}
		case JSONNodeType.Object:
		{
			int num = aReader.ReadInt32();
			JSONObject jSONObject = new JSONObject();
			for (int i = 0; i < num; i++)
			{
				string aKey = aReader.ReadString();
				JSONNode aItem = Deserialize(aReader);
				jSONObject.Add(aKey, aItem);
			}
			return jSONObject;
		}
		case JSONNodeType.String:
			return new JSONString(aReader.ReadString());
		case JSONNodeType.Number:
			return new JSONNumber(aReader.ReadDouble());
		case JSONNodeType.Boolean:
			return new JSONBool(aReader.ReadBoolean());
		case JSONNodeType.NullValue:
			return new JSONNull();
		default:
			throw new Exception("Error deserializing JSON. Unknown tag: " + jSONNodeType);
		}
	}

	public static JSONNode LoadFromCompressedFile(string aFileName)
	{
		throw new Exception("Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
	}

	public static JSONNode LoadFromCompressedStream(Stream aData)
	{
		throw new Exception("Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
	}

	public static JSONNode LoadFromCompressedBase64(string aBase64)
	{
		throw new Exception("Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
	}

	public static JSONNode LoadFromStream(Stream aData)
	{
		using BinaryReader aReader = new BinaryReader(aData);
		return Deserialize(aReader);
	}

	public static JSONNode LoadFromFile(string aFileName)
	{
		using FileStream aData = File.OpenRead(aFileName);
		return LoadFromStream(aData);
	}

	public static JSONNode LoadFromBase64(string aBase64)
	{
		return LoadFromStream(new MemoryStream(Convert.FromBase64String(aBase64))
		{
			Position = 0L
		});
	}
}
