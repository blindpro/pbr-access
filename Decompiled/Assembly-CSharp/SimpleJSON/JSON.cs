namespace SimpleJSON;

public static class JSON
{
	public static JSONNode Parse(string aJSON)
	{
		return JSONNode.Parse(aJSON);
	}

	public static JSONNode ParseEx(string aJSON)
	{
		return JSONNode.ParseEx(aJSON);
	}
}
