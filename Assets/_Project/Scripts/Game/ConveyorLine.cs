using System.Collections.Generic;
using UnityEngine;

namespace GoldenWrap.Game
{
    public sealed class ConveyorLine : MonoBehaviour
    {
        [SerializeField] private LineId lineId = LineId.A;
        [SerializeField] private float speed = 1.0f;
        [SerializeField] private float spawnInterval = 1.0f;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform despawnX;
        [SerializeField] private GameObject prefabCandy;
        [SerializeField] private GameObject prefabFoil;

        private readonly List<Slot> activeSlots = new List<Slot>(16);
        private readonly Stack<Transform> candyPool = new Stack<Transform>();
        private readonly Stack<Transform> foilPool = new Stack<Transform>();
        private readonly Stack<Transform> slotRootPool = new Stack<Transform>();

        private float spawnTimer;
        private float despawnBoundaryX;
        private string slotName;

        public LineId LineId => lineId;

        private void Awake()
        {
            slotName = lineId.ToString() + "_Slot";
            if (despawnX != null)
            {
                despawnBoundaryX = despawnX.position.x;
            }
            else
            {
                despawnBoundaryX = float.PositiveInfinity;
            }
        }

        private void OnEnable()
        {
            spawnTimer = 0f;
        }

        private void Update()
        {
            var deltaTime = Time.deltaTime;
            if (spawnInterval > 0f)
            {
                spawnTimer += deltaTime;
                while (spawnTimer >= spawnInterval)
                {
                    spawnTimer -= spawnInterval;
                    SpawnSlot();
                }
            }

            if (despawnX != null)
            {
                despawnBoundaryX = despawnX.position.x;
            }

            var movement = speed * deltaTime;
            for (var i = 0; i < activeSlots.Count; i++)
            {
                var slot = activeSlots[i];
                var root = slot.Root;
                var position = root.position;
                position.x += movement;
                root.position = position;

                if (position.x > despawnBoundaryX)
                {
                    DespawnSlot(i);
                    i--;
                }
            }
        }

        public void SetSpeed(float newSpeed)
        {
            speed = newSpeed;
        }

        private void SpawnSlot()
        {
            if (spawnPoint == null || prefabCandy == null || prefabFoil == null)
            {
                return;
            }

            if (despawnX != null)
            {
                despawnBoundaryX = despawnX.position.x;
            }

            var slotRoot = AcquireSlotRoot();
            var rootPosition = spawnPoint.position;
            slotRoot.position = rootPosition;

            var candy = AcquireCandy();
            candy.SetParent(slotRoot, false);
            candy.localPosition = Vector3.zero;

            var foil = AcquireFoil();
            foil.SetParent(slotRoot, false);
            foil.localPosition = Vector3.zero;

            activeSlots.Add(new Slot(slotRoot, candy, foil));
        }

        private Transform AcquireSlotRoot()
        {
            Transform root;
            if (slotRootPool.Count > 0)
            {
                root = slotRootPool.Pop();
                root.gameObject.SetActive(true);
            }
            else
            {
                var go = new GameObject(slotName);
                root = go.transform;
            }

            root.SetParent(transform, false);
            return root;
        }

        private Transform AcquireCandy()
        {
            Transform candy;
            if (candyPool.Count > 0)
            {
                candy = candyPool.Pop();
                candy.gameObject.SetActive(true);
            }
            else
            {
                candy = Instantiate(prefabCandy).transform;
            }

            return candy;
        }

        private Transform AcquireFoil()
        {
            Transform foil;
            if (foilPool.Count > 0)
            {
                foil = foilPool.Pop();
                foil.gameObject.SetActive(true);
            }
            else
            {
                foil = Instantiate(prefabFoil).transform;
            }

            return foil;
        }

        private void DespawnSlot(int index)
        {
            var slot = activeSlots[index];
            activeSlots.RemoveAt(index);

            ReleaseToPool(slot.Candy, candyPool);
            ReleaseToPool(slot.Foil, foilPool);

            slot.Root.SetParent(transform, false);
            slot.Root.gameObject.SetActive(false);
            slotRootPool.Push(slot.Root);
        }

        private void ReleaseToPool(Transform item, Stack<Transform> pool)
        {
            item.SetParent(transform, false);
            item.gameObject.SetActive(false);
            pool.Push(item);
        }

        private readonly struct Slot
        {
            public readonly Transform Root;
            public readonly Transform Candy;
            public readonly Transform Foil;

            public Slot(Transform root, Transform candy, Transform foil)
            {
                Root = root;
                Candy = candy;
                Foil = foil;
            }
        }
    }
}
