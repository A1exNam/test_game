using System.Collections.Generic;
using GoldenWrap.Settings;
using UnityEngine;

namespace GoldenWrap.Game
{
    public sealed class ConveyorManager : MonoBehaviour
    {
        [SerializeField] private List<ConveyorLine> lines = new List<ConveyorLine>();

        public void Init(GameConfig config)
        {
            if (config == null)
            {
                return;
            }

            var speed = config.InitialConveyorSpeed;
            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (line != null)
                {
                    line.SetSpeed(speed);
                }
            }
        }
    }
}
