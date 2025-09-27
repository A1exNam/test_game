using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GW.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class SealZone : MonoBehaviour
    {
        [SerializeField]
        private ConveyorLineController line;

        [SerializeField]
        private BlissController blissController;

        private readonly List<CandyActor> candiesInZone = new();
        private Collider2D triggerCollider;

        private void Awake()
        {
            triggerCollider = GetComponent<Collider2D>();
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }

            if (line != null)
            {
                BindLine(line);
            }

            if (blissController != null && line != null)
            {
                blissController.BindLine(line);
            }
        }

        private void Update()
        {
            if (line == null)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }

                AttemptSeal();
            }
        }

        public void BindLine(ConveyorLineController controller)
        {
            if (line == controller)
            {
                return;
            }

            if (controller == null)
            {
                line = null;
                candiesInZone.Clear();
                if (blissController != null)
                {
                    blissController.BindLine(null);
                }
                return;
            }

            line = controller;

            if (blissController != null)
            {
                blissController.BindLine(line);
            }
        }

        public void BindBlissController(BlissController controller)
        {
            blissController = controller;
            if (blissController != null && line != null)
            {
                blissController.BindLine(line);
            }
        }

        public void UnbindLine(ConveyorLineController controller)
        {
            if (line != controller)
            {
                return;
            }

            line = null;
            candiesInZone.Clear();
        }

        private void AttemptSeal()
        {
            CleanupInactive();
            if (candiesInZone.Count == 0)
            {
                return;
            }

            CandyActor targetCandy = null;
            var bestOffset = float.MaxValue;

            foreach (var candy in candiesInZone)
            {
                if (candy == null || !candy.IsActive)
                {
                    continue;
                }

                var offset = Mathf.Abs(line.CalculateOffsetFromSealPoint(candy.transform.position));
                if (offset < bestOffset)
                {
                    bestOffset = offset;
                    targetCandy = candy;
                }
            }

            if (targetCandy == null)
            {
                return;
            }

            var signedOffset = line.CalculateOffsetFromSealPoint(targetCandy.transform.position);
            signedOffset = ApplyBlissAutoSnap(signedOffset);
            line.ProcessSealAttempt(targetCandy, signedOffset);
        }

        private float ApplyBlissAutoSnap(float offset)
        {
            if (line?.Judge == null || blissController == null || !blissController.IsActive)
            {
                return offset;
            }

            var judge = line.Judge;
            var snapThreshold = judge.PerfectWindow * (1f + blissController.AutoSnapPercentage);
            if (Mathf.Abs(offset) <= snapThreshold)
            {
                return 0f;
            }

            return offset;
        }

        private void CleanupInactive()
        {
            for (var i = candiesInZone.Count - 1; i >= 0; i--)
            {
                if (candiesInZone[i] == null || !candiesInZone[i].IsActive)
                {
                    candiesInZone.RemoveAt(i);
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.TryGetComponent(out CandyActor candy))
            {
                return;
            }

            if (!candiesInZone.Contains(candy))
            {
                candiesInZone.Add(candy);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.TryGetComponent(out CandyActor candy))
            {
                return;
            }

            candiesInZone.Remove(candy);
        }
    }
}
