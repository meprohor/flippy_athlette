using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
	Vector3 offset;
	
	void Start()
	{
		offset = transform.position - GameManager.instance.player.transform.position;
	}
	
	void LateUpdate()
	{
		transform.position = GameManager.instance.player.transform.position + offset;
	}
}
