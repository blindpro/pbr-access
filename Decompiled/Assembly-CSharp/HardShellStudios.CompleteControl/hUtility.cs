using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using UnityEngine;

namespace HardShellStudios.CompleteControl;

public static class hUtility
{
	public static string SaveName = "KeyBindingsV7.xml";

	public const string DefaultName = "KeyBindings";

	public static string DefaultSchemeName = "";

	public static string LoadedSchemeName = "";

	public static hScheme GetDefaultScheme()
	{
		string text = "";
		if (Input.GetJoystickNames().Length != 0)
		{
			string text2 = Input.GetJoystickNames()[0];
			if (text2.Contains("xbox") || text2.Contains("Xbox") || text2.Contains("XBOX"))
			{
				text = "XboxOne";
				if (text2.Contains("360"))
				{
					text = "Xbox360";
				}
			}
			if (text2.Contains("Wireless Controller"))
			{
				text = "PS4";
				if (Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXEditor)
				{
					text = "PS4Mac";
				}
			}
			if (Application.platform == RuntimePlatform.LinuxPlayer || Application.platform == RuntimePlatform.LinuxEditor)
			{
				text = "Linux";
			}
		}
		try
		{
			DefaultSchemeName = "KeyBindings" + text;
			Debug.Log("Input loading default sheme:" + DefaultSchemeName);
			return (hScheme)Resources.Load(DefaultSchemeName);
		}
		catch
		{
			Debug.LogError("No '" + DefaultSchemeName + "' found inside a Resources folder.");
		}
		return null;
	}

	public static string GetSavePath()
	{
		return Application.persistentDataPath + "/" + SaveName;
	}

	public static void SaveBinings(hInputDetails[] inputs)
	{
		Debug.Log("save input to " + GetSavePath());
		XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
		xmlWriterSettings.OmitXmlDeclaration = true;
		xmlWriterSettings.Indent = true;
		XmlWriter xmlWriter = XmlWriter.Create(GetSavePath(), xmlWriterSettings);
		xmlWriter.WriteStartElement("KeyBindings");
		xmlWriter.WriteStartElement("DefaultSchemeName");
		xmlWriter.WriteAttributeString("Name", DefaultSchemeName);
		xmlWriter.WriteEndElement();
		for (int i = 0; i < inputs.Length; i++)
		{
			hInputDetails hInputDetails2 = inputs[i];
			xmlWriter.WriteStartElement("Input");
			xmlWriter.WriteAttributeString("Name", hInputDetails2.Name);
			xmlWriter.WriteAttributeString("UniqueName", hInputDetails2.UniqueName);
			int type = (int)hInputDetails2.Type;
			xmlWriter.WriteAttributeString("Type", type.ToString());
			type = (int)hInputDetails2.Positive.Primary;
			xmlWriter.WriteAttributeString("PositivePrimary", type.ToString());
			type = (int)hInputDetails2.Positive.Secondary;
			xmlWriter.WriteAttributeString("PositiveSecondary", type.ToString());
			type = (int)hInputDetails2.Negative.Primary;
			xmlWriter.WriteAttributeString("NegativePrimary", type.ToString());
			type = (int)hInputDetails2.Negative.Secondary;
			xmlWriter.WriteAttributeString("NegativeSecondary", type.ToString());
			type = (int)hInputDetails2.targetController;
			xmlWriter.WriteAttributeString("TargetController", type.ToString());
			type = (int)hInputDetails2.Axis;
			xmlWriter.WriteAttributeString("Axis", type.ToString());
			xmlWriter.WriteAttributeString("Invert", hInputDetails2.Invert.ToString());
			xmlWriter.WriteAttributeString("Sensitivity", hInputDetails2.Sensitivity.ToString());
			xmlWriter.WriteEndElement();
		}
		xmlWriter.WriteFullEndElement();
		xmlWriter.WriteEndDocument();
		xmlWriter.Close();
	}

	public static int GetUniqueIndex(string uniqueKeyName, hInputDetails[] inputs)
	{
		for (int i = 0; i < inputs.Length; i++)
		{
			if (inputs[i].UniqueName.Equals(uniqueKeyName))
			{
				return i;
			}
		}
		return -1;
	}

	public static hInputDetails[] LoadBindings(ref hInputDetails[] details)
	{
		try
		{
			hInputDetails[] array = details;
			if (new FileInfo(GetSavePath()).Exists)
			{
				Debug.Log("load input from " + GetSavePath());
				XDocument xDocument = XDocument.Load(GetSavePath());
				foreach (XElement item in xDocument.Descendants("DefaultSchemeName"))
				{
					LoadedSchemeName = item.Attribute("Name").Value;
				}
				IEnumerable<XElement> enumerable = xDocument.Descendants("Input");
				int num = 0;
				foreach (XElement item2 in enumerable)
				{
					if (num < details.Length && item2.Attribute("Name").Value == details[num].Name)
					{
						array[num].Name = details[num].Name;
						array[num].UniqueName = details[num].UniqueName;
						array[num].Type = (KeyType)int.Parse(item2.Attribute("Type").Value);
						array[num].Positive.Primary = (KeyCode)int.Parse(item2.Attribute("PositivePrimary").Value);
						array[num].Positive.Secondary = (KeyCode)int.Parse(item2.Attribute("PositiveSecondary").Value);
						array[num].Negative.Primary = (KeyCode)int.Parse(item2.Attribute("NegativePrimary").Value);
						array[num].Negative.Secondary = (KeyCode)int.Parse(item2.Attribute("NegativeSecondary").Value);
						array[num].targetController = (TargetController)int.Parse(item2.Attribute("TargetController").Value);
						array[num].Axis = (AxisCode)int.Parse(item2.Attribute("Axis").Value);
						array[num].Invert = item2.Attribute("Invert").Value == "True";
						array[num].Sensitivity = float.Parse(item2.Attribute("Sensitivity").Value);
					}
					num++;
				}
				return array;
			}
			Debug.LogWarning("No Saved Bindings Found");
		}
		catch
		{
			Debug.LogWarning("Bindings Error");
		}
		return null;
	}
}
