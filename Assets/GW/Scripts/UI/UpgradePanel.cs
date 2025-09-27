using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GW.Gameplay;

namespace GW.UI
{
    [DisallowMultipleComponent]
    public sealed class UpgradePanel : MonoBehaviour
    {
        [SerializeField]
        private Text creditsText;

        [SerializeField]
        private string creditsFormat = "Credits: {0:N0}";

        [SerializeField]
        private List<UpgradeNodeView> nodeViews = new();

        private UpgradeSystem system;
        private bool subscribed;

        private void OnEnable()
        {
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void BindSystem(UpgradeSystem upgradeSystem)
        {
            if (system == upgradeSystem)
            {
                RefreshViews();
                RefreshCredits();
                return;
            }

            Unsubscribe();
            system = upgradeSystem;

            foreach (var view in nodeViews)
            {
                view?.Bind(system);
            }

            Subscribe();
            Refresh();
        }

        public void UnbindSystem(UpgradeSystem upgradeSystem)
        {
            if (system != upgradeSystem)
            {
                return;
            }

            Unsubscribe();
            system = null;

            foreach (var view in nodeViews)
            {
                view?.Bind(null);
            }

            Refresh();
        }

        private void Subscribe()
        {
            if (system == null || subscribed)
            {
                return;
            }

            system.StateChanged += HandleStateChanged;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (system == null || !subscribed)
            {
                return;
            }

            system.StateChanged -= HandleStateChanged;
            subscribed = false;
        }

        private void HandleStateChanged()
        {
            Refresh();
        }

        private void Refresh()
        {
            RefreshCredits();
            RefreshViews();
        }

        private void RefreshCredits()
        {
            if (creditsText == null)
            {
                return;
            }

            var credits = system?.AvailableCredits ?? 0;
            creditsText.text = string.Format(creditsFormat, credits);
        }

        private void RefreshViews()
        {
            foreach (var view in nodeViews)
            {
                view?.Refresh();
            }
        }
    }
}
