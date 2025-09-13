using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
	[SerializeField] private int _health = 3;

	[Header("Patrol / Movement")]
	[SerializeField] private Vector3 _initialDirection = Vector3.right;
	[SerializeField] private float _moveSpeed = 1f;
	[SerializeField] private float _initialDelayMin = 0f;
	[SerializeField] private float _initialDelayMax = 3f;

	[Header("Detection")]
	[SerializeField] private float _visionRadius = 2f;
	[SerializeField] private float _minPlayerDistance = 0.5f;

	[Header("Distance Adjustment")]
	[SerializeField] private float _minDistanceAdjustDelay = 0.2f;
	private float _distanceAdjustTimer = 0f;

	[Header("Attack")]
	[SerializeField] private GameObject _attackHitBox;
	[SerializeField] private float _attackDuration = 0.5f;
	[SerializeField] private float _attackCooldown = 1f;
	[SerializeField] private float _attackRange = 0.5f;

	[Header("Dependencies")]
	[SerializeField] private Transform _visualRoot;
	[SerializeField] private Rigidbody _rigidbody;
	[SerializeField] private Animator _animator;
	
	private enum State { Patrol, Chase, Attack, Idle }
	private State _state = State.Patrol;

	private float _lastAttackTime = -999f;
	private float _initialDelay = 0f;
	private Vector3 _direction;
	private Transform _targetPlayer;
	private bool _isInitialized = false;

	private void Awake()
	{
		InitializeComponents();
		_direction = _initialDirection.normalized;
		_initialDelay = Random.Range(_initialDelayMin, _initialDelayMax);
		StartCoroutine(StateLoop());
	}

	private void InitializeComponents()
	{
		if (_rigidbody == null)
		{
			_rigidbody = GetComponent<Rigidbody>();
		}

		if (_animator == null)
		{
			_animator = GetComponent<Animator>();
		}

		_isInitialized = true;
	}

	private IEnumerator StateLoop()
	{
		// Espera inicial para desincronizar enemigos
		if (_initialDelay > 0f)
			yield return new WaitForSeconds(_initialDelay);

		while (true)
		{
			DetectPlayer();

			switch (_state)
			{
				case State.Patrol:
					// Si detectó player, cambiará a Chase desde DetectPlayer()
					break;
				case State.Chase:
					// si estamos dentro del rango de ataque, iniciar ataque
					if (_targetPlayer != null)
					{
						float dist = Vector3.Distance(transform.position, _targetPlayer.position);
						float effectiveAttackRange = (_attackRange > 0f) ? _attackRange : Mathf.Max(_minPlayerDistance, 0.1f);
						if (dist <= effectiveAttackRange && CanAttack())
						{
							StartCoroutine(ExecuteAttack());
						}
					}
					break;
				case State.Attack:
					// la corutina de ataque maneja el estado temporariamente
					break;
				case State.Idle:
					// breve pausa antes de volver a patrullar
					yield return new WaitForSeconds(0.5f);
					_state = State.Patrol;
					break;
			}

			yield return null;
		}
	}

	private void Update()
	{
		if (_state == State.Patrol)
		{
			UpdateFacing(_direction.x);
			Move(_direction);
			if (_animator != null)
			{
				_animator.SetTrigger("isWalk");
			}
		}
		else if (_state == State.Chase && _targetPlayer != null)
		{
			Vector3 toPlayer = _targetPlayer.position - transform.position;
			float dist = toPlayer.magnitude;
			Vector3 dir = toPlayer.normalized;

			// deadzone para evitar jitter
			const float margin = 0.05f;

			if (dist > _minPlayerDistance + margin)
			{
				// acumular tiempo antes de acercarse
				_distanceAdjustTimer += Time.deltaTime;
				if (_distanceAdjustTimer >= _minDistanceAdjustDelay)
				{
					// ajustar facing según dirección al jugador
					UpdateFacing(dir.x);
					Move(dir);
					if (_animator != null)
					{
						_animator.SetTrigger("isWalk");
					}
				}
			}
			else if (dist < _minPlayerDistance - margin)
			{
				// acumular tiempo antes de retroceder
				_distanceAdjustTimer += Time.deltaTime;
				if (_distanceAdjustTimer >= _minDistanceAdjustDelay)
				{
					// ajustar facing según dirección opuesta (retroceso)
					UpdateFacing(-dir.x);
					Move(-dir);
					if (_animator != null)
					{
						_animator.SetTrigger("isWalk");
					}
				}
			}
			else
			{
				// estamos dentro del rango de mantenimiento -> dejar de moverse y resetear timer
				_distanceAdjustTimer = 0f;
				if (_animator != null)
				{
					_animator.SetTrigger("isWalk"); //isIdle
				}
			}
		}
	}

	private void Move(Vector3 dir)
	{
		Vector3 movement = dir.normalized * _moveSpeed * Time.deltaTime;
		if (_rigidbody != null)
		{
			_rigidbody.MovePosition(_rigidbody.position + movement);
		}
		else
		{
			transform.position += movement;
		}
	}

	private void DetectPlayer()
	{
		Collider[] hits = Physics.OverlapSphere(transform.position, _visionRadius);
		Transform found = null;
		foreach (var hit in hits)
		{
			if (hit.CompareTag("Player"))
			{
				// Selecciona el primer jugador encontrado
				found = hit.transform;
				break;
			}
		}

		if (found != null)
		{
			_targetPlayer = found;
			_state = State.Chase;
		}
		else
		{
			_targetPlayer = null;
			_state = State.Patrol;
		}
	}

	public void OnTriggerEnter(Collider other)
	{
		if (other.gameObject.CompareTag("Attack"))
		{
			TakeDamage(1, other.transform);
		}
	}

	private void TakeDamage(int amount, Transform attacker)
	{
		_health -= amount;

		if (_animator != null)
		{
			_animator.SetTrigger("isDamage");
		}

		if (_health <= 0)
		{
			Die();
		}
	}

	private void Die()
	{
		if (_animator != null)
		{
			_animator.SetTrigger("isDeath");
		}
		Destroy(gameObject);
	}

	private bool CanAttack()
	{
		return Time.time >= _lastAttackTime + _attackCooldown;
	}

	private IEnumerator ExecuteAttack()
	{
		_state = State.Attack;
		_lastAttackTime = Time.time;

		if (_animator != null)
		{
			_animator.SetTrigger("isAttack");
		}

		if (_attackHitBox != null)
		{
			_attackHitBox.SetActive(true);
		}

		yield return new WaitForSeconds(_attackDuration);

		if (_attackHitBox != null)
		{
			_attackHitBox.SetActive(false);
		}

		if (_animator != null)
		{
			_animator.SetTrigger("isWalk"); //isIdle
		}

		// Volver a perseguir o patrullar según si aún hay objetivo
		_state = (_targetPlayer != null) ? State.Chase : State.Patrol;
	}

	private void OnDrawGizmosSelected()
	{
		// Mostrar radio de visión en el editor
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(transform.position, _visionRadius);

		// Mostrar rango de ataque también para debug
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(transform.position, (_attackRange > 0f) ? _attackRange : _minPlayerDistance);
	}

	/// <summary>
	/// Gira el visualRoot 180º en Y cuando la componente X de la dirección es negativa.
	/// Evita cambiar escala del objeto raíz para no afectar colliders.
	/// </summary>
	private void UpdateFacing(float dirX)
	{
		if (_visualRoot == null)
		{
			// intentar encontrar un hijo con SpriteRenderer si no se asignó
			if (transform.childCount > 0)
			{
				for (int i = 0; i < transform.childCount; i++)
				{
					var child = transform.GetChild(i);
					if (child.GetComponent<SpriteRenderer>() != null)
					{
						_visualRoot = child;
						break;
					}
				}
			}
			if (_visualRoot == null) return;
		}

		const float turnThreshold = 0.1f;
		Vector3 euler = _visualRoot.localEulerAngles;
		if (dirX < -turnThreshold)
		{
			// mirar hacia la izquierda
			euler.y = 180f;
			_visualRoot.localEulerAngles = euler;
		}
		else if (dirX > turnThreshold)
		{
			// mirar hacia la derecha
			euler.y = 0f;
			_visualRoot.localEulerAngles = euler;
		}
	}
}
