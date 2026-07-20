using UnityEngine;

public class PlayerMover : MonoBehaviour
{
	public float moveForce = 1f;

	public float jumpForce = 100f;

	private void Update()
	{
		if (hInput.GetButtonDown("Jump"))
		{
			GetComponent<Rigidbody2D>().AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
		}
		GetComponent<Rigidbody2D>().velocity = new Vector2(hInput.GetAxis("Horizontal") * moveForce, GetComponent<Rigidbody2D>().velocity.y);
		Debug.Log("X: " + hInput.GetAxis("Horizontal") + "  Y: " + hInput.GetAxis("Vertical") + "             Mouse X: " + hInput.GetAxis("Mouse X") + "  Mouse Y: " + hInput.GetAxis("Mouse Y"));
	}
}
