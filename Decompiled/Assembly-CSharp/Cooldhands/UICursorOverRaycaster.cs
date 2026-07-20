using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Cooldhands;

public class UICursorOverRaycaster : BaseRaycaster
{
	public static PointerEventData LastPointerEventData;

	public override Camera eventCamera => null;

	protected override void Awake()
	{
		base.Awake();
		LastPointerEventData = null;
	}

	public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
	{
		LastPointerEventData = eventData;
	}
}
