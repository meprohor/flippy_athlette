/*
 * GameManager.cs - Flippy_Athlette main manager
 * keeps track of player score, pool object's usage
 * and spawning new platforms
 *
 * Author - meprohor (meprohor@gmail.com)
 * License - wtfpl v.2
 */

/* MonoBehaviour */
using UnityEngine;
/* Text */
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
	/* VARIABLES */
	/* singleton; easy reference to single class instance */
	private static GameManager _instance;
	public static GameManager instance
	{
		private set { _instance = value; }
		get { return _instance; }
	}
	
	/* viewport position of input start and result vector */
	private Vector2 startTouchPos,
		swipeVector;
	/* force applied to player modifier */
	public float forceMultiplier = 1500.0f;
	
	/* Text components references */
	public Text scoreText,
		topScoreText;
	
	/* currently scored value and maximum score achieved */
	private int _currentScore = 0,
		_topScore = 0;
	public int currentScore
	{
		set
		{
			_currentScore = value;
			scoreText.text = "Score: " + _currentScore;
			
			/* change top score value if exceeded */
			if(_topScore < _currentScore)
			{
				_topScore = _currentScore;
				topScoreText.text = "(Top Score: " + _topScore + ")";
			}
		}
		get { return _currentScore; }
	}
	public int topScore
	{
		get { return _topScore; }
	}
	
	/* number of gameobjects in pools */
	private int poolCount = 2;
	
	/* pool of platforms gameobjects */
	private GameObject[] platformPool;
	/* pool of particles appering on platform destruction */
	private GameObject[] particlePool;
	
	/* references to platform pool's Material components  */
	private Material[] platformMaterials;
	/* references to particle pool's ParticleSystem components  */
	private ParticleSystem[] particleComponents;
	
	/* vector towards camera used to calculate next platforms direction */
	private Vector3 _cameraDirection;
	private Vector3 cameraDirection
	{
		get
		{
			if(Vector3.zero == _cameraDirection)
			{
				_cameraDirection = Camera.main.transform.position - platformPool[curInstanceIndex].transform.position;
				_cameraDirection.y = .0f;
				_cameraDirection = Vector3.Normalize(_cameraDirection);
			}
			return _cameraDirection;
		}
	}
	
	/* vectors where next platform could appear */
	private Vector3 _sixtyDegrees,
		_thirtyDegrees;
	private Vector3 sixtyDegrees
	{
		get
		{
			if(Vector3.zero == _sixtyDegrees)
				_sixtyDegrees = Vector3.Normalize(cameraDirection + 2.0f * Camera.main.transform.right);
			return _sixtyDegrees;
		}
	}
	private Vector3 thirtyDegrees
	{
		get
		{
			if(Vector3.zero == _thirtyDegrees)
				_thirtyDegrees = Vector3.Normalize(2.0f * cameraDirection - Camera.main.transform.right);
			return _thirtyDegrees;
		}
	}
	
	/* whether next platform is on screen's right or not */
	private bool rightDirection;
	
	/* reference to Env GameObject to parent pool members*/
	public GameObject enviromentGameObject;
	
	/* currently used pool's instance index */
	private int _curInstanceIndex = 0;
	private int curInstanceIndex
	{
		set { _curInstanceIndex = value % poolCount; }
		get { return _curInstanceIndex; }
	}
	/* next pool's instance to use */
	private int nextInstanceIndex
	{
		get { return ((curInstanceIndex + 1) % poolCount); }
	}
	
	/* minimum and maximum distance next platform could be spawned at */
	public float minDistance = 3.0f,
		maxDistance = 4.0f,
	/* maximum possible platform height change */
		deltaY = 3.0f;
	
	/* external player reference and its components */
	public GameObject player;
	private Rigidbody _playerRigidbody;
	private Rigidbody playerRigidbody
	{
		get
		{
			if(null == _playerRigidbody)
				_playerRigidbody = player.GetComponent<Rigidbody>();
			return _playerRigidbody;
		}
	}
	private SpriteRenderer _playerSpriteRenderer;
	private SpriteRenderer playerSpriteRenderer
	{
		get
		{
			if(null == _playerSpriteRenderer)
				_playerSpriteRenderer = player.GetComponentInChildren<SpriteRenderer>();
			return _playerSpriteRenderer;
		}
	}
	public ParticleSystem playerParticleSystem;
	
	/* sprites of player standing and flying */
	public Sprite groundedSprite,
		flyingSprite;
	
	/* whether player is in air or not, his launch time and maximum time in air before gameover */
	private bool inAir = true;
	private float startFlyTime;
	public float maxFlyTime = 5.0f;
	
	/* number of radians passed by player while rotating in air */
	public float rotationSpeed = 150.0f;
	
	/* METHODS */
	/* as soon as scene starts resolve internal independent variables */
	void Awake()
	{
		/* destroy if more than one copy of such component exists  */
		if(null == instance)
			instance = this;
		else
			Destroy(gameObject);
	}
	
	private Color RandomColorHue()
	{
		return Random.ColorHSV(.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f);
	}
	
	/* resolve preparations concerning external references */
	void Start()
	{
		/* execute only in editor */
		#if UNITY_EDITOR
		if(null == scoreText
			|| null == topScoreText
			|| null == groundedSprite
			|| null == flyingSprite
			|| null == enviromentGameObject
			|| null == player)
		{
			Debug.LogError("Not all GameManagement external references are set!");
			Debug.Break();
		}
		#endif
		
		/* initialize pools */
		platformPool = new GameObject[poolCount];
		particlePool = new GameObject[poolCount];
		
		platformMaterials = new Material[poolCount];
		particleComponents = new ParticleSystem[poolCount];
		
		/* reference resource data by name */
		GameObject prefab = Resources.Load("Prefabs/Platform") as GameObject;
		GameObject particlePrefab = Resources.Load("Prefabs/DestructionPS") as GameObject;
		
		bool passedFirstIteration = false;
		
		/* prepare pool objects to be used */
		for(int i = 0; i < poolCount; i ++)
		{
			/* create pool object */
			platformPool[i] = Instantiate(prefab, Vector3.zero, prefab.transform.rotation,
				enviromentGameObject.transform) as GameObject;
			particlePool[i] = Instantiate(particlePrefab, Vector3.zero, particlePrefab.transform.rotation,
				enviromentGameObject.transform) as GameObject;
			
			platformMaterials[i] = platformPool[i].GetComponent<Renderer>().material;
			particleComponents[i] = particlePool[i].GetComponent<ParticleSystem>();
			
			/* disable all pool objects beyond first one */
			if(passedFirstIteration)
				platformPool[i].SetActive(false);
			else
			{
				/* prepare first platform to be used */
				platformMaterials[0].color = RandomColorHue();
				passedFirstIteration = true;
			}
		}
		
		/* face player towards camera */
		player.transform.forward = -1.0f * Camera.main.transform.forward;
		
		/* prepare player's landing particles */
		var main = playerParticleSystem.main;
		main.startColor = platformMaterials[0].color;
	}
	
	/* executed at gameover */
	void LevelRestart()
	{
		currentScore = 0;
		startFlyTime = Time.time;
		
		playerRigidbody.velocity = Vector3.zero;
		playerRigidbody.angularVelocity = Vector3.zero;
		
		for(int i = 1; i < poolCount; i ++)
			platformPool[i].SetActive(false);
		
		platformPool[0].transform.position = Vector3.zero;
		platformMaterials[0].color = RandomColorHue();
		platformPool[0].SetActive(true);
		
		player.transform.position = new Vector3(.0f, 6.0f, .0f);
		
		var main = playerParticleSystem.main;
		main.startColor = platformMaterials[0].color;
		
		curInstanceIndex = 0;
	}
	
	/* returns next platform position after currently used */
	private Vector3 NextPosition()
	{
		Transform curTransform = platformPool[curInstanceIndex].transform;
		Vector3 result;
		
		if(Random.value > .5f)
		{
			result = thirtyDegrees;
			rightDirection = true;
		}
		else
		{
			result = sixtyDegrees;
			rightDirection = false;
		}
		
		result = platformPool[curInstanceIndex].transform.position + result * Random.Range(minDistance, maxDistance);
		/* offset height */
		result.y += Random.Range(- deltaY, deltaY);
		
		return result;
	}
	
	/* executed when player jumps */
	public void OnLaunch()
	{
		inAir = true;
		startFlyTime = Time.time;
		playerSpriteRenderer.sprite = flyingSprite;
		
		particlePool[curInstanceIndex].transform.position = platformPool[curInstanceIndex].transform.position;
		
		var main = particleComponents[curInstanceIndex].main;
		main.startColor = platformMaterials[curInstanceIndex].color;
		
		main = playerParticleSystem.main;
		main.startColor = platformMaterials[nextInstanceIndex].color;
		
		/* 'destroy' platform behind */
		platformPool[curInstanceIndex].SetActive(false);
		particleComponents[curInstanceIndex].Play();
		
		curInstanceIndex ++;
	}
	
	/* executed when player lands */
	public void OnLanding()
	{
		inAir = false;
		playerSpriteRenderer.sprite = groundedSprite;
		/* reset rotation */
		player.transform.up = Camera.main.transform.up;
		
		playerParticleSystem.transform.position = player.transform.position;
		playerParticleSystem.Play();
		
		currentScore ++;
		
		/* place next platform */
		platformPool[nextInstanceIndex].transform.position = NextPosition();
		platformMaterials[nextInstanceIndex].color = RandomColorHue();
		platformPool[nextInstanceIndex].SetActive(true);
	}
	
	/* Input check at every frame */
	void Update()
	{
		/* jump cannot be performed in air */
		if(!inAir)
		{
			if(Input.GetButtonDown("Touch"))
				startTouchPos = (Vector2)Camera.main.ScreenToViewportPoint(Input.mousePosition);
			else if(Input.GetButtonUp("Touch"))
			{
				swipeVector = (Vector2)Camera.main.ScreenToViewportPoint(Input.mousePosition) - startTouchPos;
				
				Vector3 direction;
				
				if((rightDirection && swipeVector.x < .0f) || (!rightDirection && swipeVector.x > .0f))
				{
					direction = platformPool[nextInstanceIndex].transform.position - player.transform.position;
					direction.y = .0f;
					direction = Vector3.Normalize(direction);
				}
				else if(rightDirection)
					direction = sixtyDegrees;
				else
					direction = thirtyDegrees;
				
				direction *= Mathf.Abs(swipeVector.x);
				direction += Vector3.up * Mathf.Abs(swipeVector.y);
				
				playerRigidbody.AddForce(direction * forceMultiplier);
			}
		}
		else
		{
			/* spin player if in air */
			player.transform.Rotate(player.transform.forward, rotationSpeed * Time.deltaTime, Space.World);
			
			/* check if gameover conditions met */
			if(Time.time - startFlyTime > maxFlyTime)
				LevelRestart();
		}
	}
}
