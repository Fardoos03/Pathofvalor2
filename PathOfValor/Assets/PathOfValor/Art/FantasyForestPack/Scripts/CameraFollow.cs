using UnityEngine;
using System.Collections;

public class CameraFollow : MonoBehaviour 
{

	#region Member Variables
	/// <summary>
	/// The maximum x and y coordinates the camera can have
	/// </summary>
	public Vector2 MAXBounds;

	/// <summary>
	/// The minimum x and y coordinates the camera can have
	/// </summary>
	public Vector2 MINBounds;

	/// <summary>
	/// The player character
	/// </summary>
	public GameObject PlayerCharacter;

	/// <summary>
	///  Reference to the users current view transform.
	/// </summary>
	private Transform PlayerTransform;

	[SerializeField]
	private string playerTag = "Player";
	#endregion

	void Start()
	{
		ResolvePlayerTransform();
	}
	
	void Update ()
	{
		if(PlayerTransform == null)
		{
			ResolvePlayerTransform();
			if(PlayerTransform == null)
			{
				return;
			}
		}

		Vector2 target = new Vector2(PlayerTransform.position.x, PlayerTransform.position.y);

		// Clamp the camera within the bounds when configured
		if(MAXBounds.x > MINBounds.x)
		{
			target.x = Mathf.Clamp(target.x, MINBounds.x, MAXBounds.x);
		}
		if(MAXBounds.y > MINBounds.y)
		{
			target.y = Mathf.Clamp(target.y, MINBounds.y, MAXBounds.y);
		}

		// Set the camera's position to the target position with the same z component.
		transform.position = new Vector3(target.x, target.y, transform.position.z);
	}

	private void ResolvePlayerTransform()
	{
		if(PlayerCharacter == null)
		{
			PlayerCharacter = FindPlayer();
		}

		if(PlayerCharacter != null)
		{
			PlayerTransform = PlayerCharacter.transform;
		}
	}

	private GameObject FindPlayer()
	{
		try
		{
			return GameObject.FindGameObjectWithTag(playerTag);
		}
		catch (UnityException)
		{
			return GameObject.Find("PlayerCharacter");
		}
	}

	public void SetPlayer(GameObject player)
	{
		PlayerCharacter = player;
		PlayerTransform = player != null ? player.transform : null;
	}
}
