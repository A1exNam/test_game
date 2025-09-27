using System.Collections.Generic;
using UnityEngine;
using GW.Gameplay;

namespace GW.UI
{
    [DisallowMultipleComponent]
    public sealed class ContractsPanel : MonoBehaviour
    {
        [SerializeField]
        private List<ContractItemView> contractSlots = new();

        private ContractSystem system;

        private void OnEnable()
        {
            if (system != null)
            {
                system.ActiveContractsChanged += HandleContractsChanged;
            }
        }

        private void OnDisable()
        {
            if (system != null)
            {
                system.ActiveContractsChanged -= HandleContractsChanged;
            }
        }

        public void BindSystem(ContractSystem contractSystem)
        {
            if (system == contractSystem)
            {
                return;
            }

            if (system != null)
            {
                system.ActiveContractsChanged -= HandleContractsChanged;
            }

            system = contractSystem;

            if (system != null)
            {
                system.ActiveContractsChanged += HandleContractsChanged;
                HandleContractsChanged(system.ActiveContracts);
            }
            else
            {
                HandleContractsChanged(null);
            }
        }

        public void UnbindSystem(ContractSystem contractSystem)
        {
            if (system != contractSystem)
            {
                return;
            }

            if (system != null)
            {
                system.ActiveContractsChanged -= HandleContractsChanged;
            }

            system = null;
            HandleContractsChanged(null);
        }

        private void HandleContractsChanged(IReadOnlyList<ContractInstance> contracts)
        {
            for (var i = 0; i < contractSlots.Count; i++)
            {
                var slot = contractSlots[i];
                if (slot == null)
                {
                    continue;
                }

                ContractInstance instance = null;
                if (contracts != null && i < contracts.Count)
                {
                    instance = contracts[i];
                }

                slot.Bind(instance);
            }
        }
    }
}
