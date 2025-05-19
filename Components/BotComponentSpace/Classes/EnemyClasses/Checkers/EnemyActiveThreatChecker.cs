using SAIN.SAINComponent.Classes.EnemyClasses;
using UnityEngine;

namespace SAIN.Components.BotComponentSpace.Classes.EnemyClasses
{
    public class EnemyActiveThreatChecker : EnemyBase, IBotClass
    {
        public bool ActiveThreat { get; private set; }

        public EnemyActiveThreatChecker(Enemy enemy) : base(enemy)
        {
        }

        public void Init()
        {
            SubscribeToDispose(Dispose);
        }

        public void Update()
        {
            checkActiveThreat();
        }

        public void Dispose()
        {
        }

        private void checkActiveThreat()
        {
            ActiveThreat = isActiveThreat();
            Enemy.Events.OnActiveThreatChanged.CheckToggle(ActiveThreat);
        }

        private bool isActiveThreat()
        {
            // If the enemy is an in-active bot or haven't sensed them in a very long time, just set them as inactive.
            if (!Enemy.EnemyKnown)
            {
                return false;
            }

            if (Enemy.IsCurrentEnemy)
            {
                return true;
            }

            // have we seen them very recently?
            if (Enemy.IsVisible || (Enemy.Seen && Enemy.TimeSinceSeen < 30f))
            {
                return true;
            }
            // have we heard them very recently?
            if (Enemy.Status.HeardRecently || (Enemy.Heard && Enemy.TimeSinceHeard < 10f))
            {
                return true;
            }
            Vector3? lastKnown = Enemy.KnownPlaces.LastKnownPosition;
            // do we have no position where we sensed an enemy?
            if (lastKnown == null)
            {
                return false;
            }
            float timeSinceActive = Enemy.TimeSinceCurrentEnemy;
            float sqrMagnitude = (lastKnown.Value - EnemyCurrentPosition).sqrMagnitude;
            if (Enemy.IsAI)
            {
                // Is the AI Enemys current position greater than x meters away from our last known position?
                if (sqrMagnitude > _activeDistanceThresholdAI * _activeDistanceThresholdAI)
                {
                    // Set them as inactive after a certain x seconds
                    return timeSinceActive < _activeForPeriodAI;
                }
                else
                {
                    // Enemy is close to where we last saw them, keep considering them as active.
                    return true;
                }
            }
            else
            {
                // Is the Human Enemys current position greater than x meters away from our last known position?
                if (sqrMagnitude > _activeDistanceThreshold * _activeDistanceThreshold)
                {
                    // Set them as inactive after a certain x seconds
                    return timeSinceActive < _activeForPeriod;
                }
                else
                {
                    // Enemy is close to where we last saw them, keep considering them as active.
                    return true;
                }
            }
        }

        private float _activeForPeriod = 180f;
        private float _activeForPeriodAI = 90f;
        private float _activeDistanceThreshold = 150f;
        private float _activeDistanceThresholdAI = 75f;
    }
}
