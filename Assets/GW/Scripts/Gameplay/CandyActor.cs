using System;
using UnityEngine;

namespace GW.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class CandyActor : MonoBehaviour
    {
        public event Action<CandyActor> Despawned;

        public bool IsActive => isActiveAndEnabled;

        private ConveyorLineController owner;
        private Vector3 direction;
        private float speed;

        public void Activate(ConveyorLineController line, Vector3 spawnPosition, Vector3 direction, float speed)
        {
            owner = line;
            this.direction = direction;
            this.speed = speed;

            transform.position = spawnPosition;
            gameObject.SetActive(true);
        }

        public void Tick(float deltaTime)
        {
            transform.position += direction * speed * deltaTime;
        }

        public void SetSpeed(float value)
        {
            speed = Mathf.Max(0f, value);
        }

        public void Despawn()
        {
            gameObject.SetActive(false);
            owner = null;
            Despawned?.Invoke(this);
        }
    }
}
