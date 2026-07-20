using Photon.Pun;
using UnityEngine;

public class NetworkTransformSynch : MonoBehaviourPun, IPunObservable
{
	[Header("Sync Settings")]
	public bool compressPosition = true;

	public bool compressRotation = true;

	public bool compressLookRotation = true;

	[Header("Smoothing")]
	public float smoothPosition = 10f;

	public float smoothRotation = 10f;

	public float teleportThreshold = 3f;

	private Vector3 targetPosition;

	private float targetRotationY;

	public Vector3 world_velocity;

	private Vector3 lastPosition;

	private void Start()
	{
		lastPosition = base.transform.position;
	}

	private void Update()
	{
		bool isMine = base.photonView.IsMine;
		if (!isMine)
		{
			if (Vector3.Distance(base.transform.position, targetPosition) > teleportThreshold)
			{
				base.transform.position = targetPosition;
			}
			else
			{
				base.transform.position = Vector3.Lerp(base.transform.position, targetPosition, Time.deltaTime * smoothPosition);
			}
			float y = Mathf.LerpAngle(base.transform.eulerAngles.y, targetRotationY, Time.deltaTime * smoothRotation);
			base.transform.rotation = Quaternion.Euler(0f, y, 0f);
		}
		if (isMine)
		{
			world_velocity = (base.transform.position - lastPosition) / Time.deltaTime;
			lastPosition = base.transform.position;
			return;
		}
		float num = 10f;
		Vector3 b = (base.transform.position - lastPosition) / Time.deltaTime;
		world_velocity = Vector3.Lerp(world_velocity, b, Time.deltaTime * num);
		lastPosition = base.transform.position;
	}

	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (stream.IsWriting)
		{
			if (compressPosition)
			{
				Vector3 position = base.transform.position;
				stream.SendNext((short)(position.x * 100f));
				stream.SendNext((short)(position.y * 100f));
				stream.SendNext((short)(position.z * 100f));
			}
			else
			{
				stream.SendNext(base.transform.position);
			}
			if (compressRotation)
			{
				byte b = (byte)(base.transform.eulerAngles.y / 360f * 255f);
				stream.SendNext(b);
			}
			else
			{
				stream.SendNext(base.transform.eulerAngles.y);
			}
		}
		else
		{
			if (compressPosition)
			{
				float x = (float)(short)stream.ReceiveNext() / 100f;
				float y = (float)(short)stream.ReceiveNext() / 100f;
				float z = (float)(short)stream.ReceiveNext() / 100f;
				targetPosition = new Vector3(x, y, z);
			}
			else
			{
				targetPosition = (Vector3)stream.ReceiveNext();
			}
			if (compressRotation)
			{
				byte b2 = (byte)stream.ReceiveNext();
				targetRotationY = (float)(int)b2 / 255f * 360f;
			}
			else
			{
				targetRotationY = (float)stream.ReceiveNext();
			}
		}
	}
}
