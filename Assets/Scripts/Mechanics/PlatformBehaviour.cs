using UnityEngine;

public class PlatformBehaviour : MonoBehaviour
{
	void OnTriggerExit(Collider other)
	{
		GameManager.instance.OnLaunch();
	}
	
	void OnTriggerEnter(Collider other)
	{
		GameManager.instance.OnLanding();
	}
}
