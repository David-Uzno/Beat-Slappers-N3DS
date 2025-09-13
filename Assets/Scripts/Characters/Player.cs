using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour
{
    [Header("Movement Limits")]
    [SerializeField] private float _maxPosition = 0.5f;
    [SerializeField] private float _minPosition = -1f;

    [Header("Movement")]
    [SerializeField] private float _speed = 5f;

    [Header("Attack")]
    [SerializeField] private GameObject _attackHitBox;
    [SerializeField] private float _attackDuration = 1f;

    [Header("Dependencies")]
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private Animator _animator;

    private bool _canMoveY = false;
    private Vector3 _lastPosition;

    private MovementHandler _movementHandler;

    private void Awake()
    {
        if (_rigidbody == null)
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
        }

        if (_rigidbody != null)
        {
            _movementHandler = new MovementHandler(_rigidbody, _speed, _minPosition, _maxPosition);
            _lastPosition = _rigidbody.position;
        }
        else
        {
            Debug.LogError("El Player requiere un Rigidbody. Asigna uno en el inspector o añade un componente Rigidbody.");
            enabled = false;
        }
    }

    private void FixedUpdate()
    {
        Move();

        if (!_canMoveY && _movementHandler != null && _rigidbody != null)
        {
            _movementHandler.HandleIdleState(_rigidbody.position);
        }

        if (_rigidbody != null)
            _lastPosition = _rigidbody.position;
    }

    private void Update()
    {
        HandleAttack();
    }

    private void Move()
    {
        if (_rigidbody == null || _movementHandler == null)
            return;

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector2 input = new Vector2(horizontal, vertical);

        Vector3 currentPosition = _rigidbody.position;

        _canMoveY = Mathf.Abs(currentPosition.z - _lastPosition.z) > Mathf.Epsilon;

        Vector3 velocity = _movementHandler.CalculateVelocity(input, currentPosition, _lastPosition, _canMoveY);
        _rigidbody.velocity = velocity;

        FlipCharacter(input.x);

        bool isWalking = Mathf.Abs(input.x) > Mathf.Epsilon || Mathf.Abs(input.y) > Mathf.Epsilon;
        _animator.SetBool("isWalk", isWalking);
    }

    private void FlipCharacter(float horizontalInput)
    {
        // Evitar escalado negativo que rompe BoxColliders.
        if (horizontalInput < 0)
        {
            transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        }
        else if (horizontalInput > 0)
        {
            transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        }
    }

    private void HandleAttack()
    {
        if (Input.GetButtonDown("Fire1") && _attackHitBox != null)
        {
            StartCoroutine(ActivateAttackHitBox());
        }
    }

    private IEnumerator ActivateAttackHitBox()
    {
        _attackHitBox.SetActive(true);
        _animator.SetTrigger("isAttack");
        yield return new WaitForSeconds(_attackDuration);
        _attackHitBox.SetActive(false);
    }

    private void PlayFootSteps()
    {
        Debug.Log("Se escuchó un sonido de pasos.");
    }
}

public class MovementHandler
{
    private readonly Rigidbody _rigidbody;
    private readonly float _speed;
    private readonly float _minPosition;
    private readonly float _maxPosition;

    public MovementHandler(Rigidbody rigidbody, float speed, float minPosition, float maxPosition)
    {
        _rigidbody = rigidbody;
        _speed = speed;
        _minPosition = minPosition;
        _maxPosition = maxPosition;
    }

    public Vector3 CalculateVelocity(Vector2 input, Vector3 currentPosition, Vector3 lastPosition, bool canMoveY)
    {
        float zVelocity = input.y * _speed;
        float clampedZ = Mathf.Clamp(currentPosition.z + zVelocity * Time.fixedDeltaTime, _minPosition, _maxPosition);
        float clampedY = CalculateClampedY(currentPosition, lastPosition, canMoveY);
        float verticalVelocity = (clampedY - currentPosition.y) / Time.fixedDeltaTime;

        return new Vector3(input.x * _speed, verticalVelocity, (clampedZ - currentPosition.z) / Time.fixedDeltaTime);
    }

    private float CalculateClampedY(Vector3 currentPosition, Vector3 lastPosition, bool canMoveY)
    {
        if (canMoveY)
        {
            float deltaZ = currentPosition.z - lastPosition.z;
            // Evite la división por cero si min y max son iguales
            float range = _maxPosition - _minPosition;
            float ratio = 0f;
            if (Mathf.Abs(range) > Mathf.Epsilon)
            {
                ratio = deltaZ * range / range;
            }
            float deltaY = ratio;
            return Mathf.Clamp(currentPosition.y + deltaY, _minPosition, _maxPosition);
        }

        return currentPosition.y;
    }

    public void HandleIdleState(Vector3 currentPosition)
    {
        // Clamp la posición Z dentro de los límites y mantener X/Y iguales.
        float clampedZ = Mathf.Clamp(currentPosition.z, _minPosition, _maxPosition);
        _rigidbody.position = new Vector3(currentPosition.x, currentPosition.y, clampedZ);
    }
}
