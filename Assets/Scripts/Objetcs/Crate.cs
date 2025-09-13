using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Crate : MonoBehaviour
{
    [SerializeField] private int _health = 3;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Attack"))
        {
            _health -= 1;
            if (_health <= 0)
            {
                Destroy(gameObject);
            }
        }
    }
}
