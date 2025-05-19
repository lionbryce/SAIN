using SAIN.Components.BotController;
using SAIN.Models.Enums;
using SAIN.Models.Structs;
using SAIN.SAINComponent.Classes.EnemyClasses;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace SAIN.Components
{
    public class BotRaycastJobs : SAINControllerBase
    {
        public VisionRaycastJob VisionJob { get; }

        public BotRaycastJobs(SAINBotController botController) : base(botController)
        {
            VisionJob = new VisionRaycastJob(botController);
        }

        public void Update()
        {
        }

        public void Dispose()
        {
            VisionJob.Dispose();
        }
    }

    public class VisionRaycastJob : SAINControllerBase
    {
        public VisionRaycastJob(SAINBotController botcontroller) : base(botcontroller)
        {
            botcontroller.StartCoroutine(CheckVisionLoop());
        }

        private IEnumerator CheckVisionLoop()
        {
            yield return null;

            while (true)
            {
                if (BotController == null)
                {
                    yield return null;
                    continue;
                }

                var bots = BotController.BotSpawnController?.BotDictionary;
                if (bots == null || bots.Count == 0)
                {
                    yield return null;
                    continue;
                }

                if (BotController.BotGame?.Status == EFT.GameStatus.Stopping)
                {
                    yield return null;
                    continue;
                }

                FindEnemies(bots, _enemies);
                int enemyCount = _enemies.Count;
                if (enemyCount == 0)
                {
                    yield return null;
                    continue;
                }

                if (_partCount < 0)
                {
                    _partCount = _enemies[0].Vision.VisionChecker.EnemyParts.PartsArray.Length;
                }
                int partCount = _partCount;

                int totalRaycasts = enemyCount * partCount * RAYCAST_CHECKS;

                _hits = new NativeArray<RaycastHit>(totalRaycasts, Allocator.TempJob);
                _commands = new NativeArray<RaycastCommand>(totalRaycasts, Allocator.TempJob);

                CreateCommands(_enemies, _commands, enemyCount, partCount);
                _handle = RaycastCommand.ScheduleBatch(_commands, _hits, 24);

                yield return null;

                _handle.Complete();
                AnalyzeHits(_enemies, _hits, enemyCount, partCount);
                _commands.Dispose();
                _hits.Dispose();
            }
        }

        public void Dispose()
        {
            if (!_handle.IsCompleted) _handle.Complete();
            if (_commands.IsCreated) _commands.Dispose();
            if (_hits.IsCreated) _hits.Dispose();
        }

        private NativeArray<RaycastHit> _hits;
        private NativeArray<RaycastCommand> _commands;
        private JobHandle _handle;

        private void CreateCommands(List<Enemy> enemies, NativeArray<RaycastCommand> raycastCommands, int enemyCount, int partCount)
        {
            _colliderTypes.Clear();
            _castPoints.Clear();

            int commands = 0;
            for (int i = 0; i < enemyCount; i++)
            {
                var enemy = _enemies[i];
                var transform = enemy.Bot.Transform;
                Vector3 eyePosition = transform.EyePosition;
                Vector3 weaponFirePort = transform.WeaponFirePort;
                var parts = enemy.Vision.VisionChecker.EnemyParts.PartsArray;
                // var partDistances = enemy.EnemyPlayerData.DistanceData.BodyPartDistances;

                for (int j = 0; j < partCount; j++)
                {
                    var part = parts[j];
                    
                    SAINBodyPartRaycast raycastData = part.GetRaycast();
                    Vector3 castPoint = raycastData.CastPoint;

                    _colliderTypes.Add(raycastData.ColliderType);
                    _castPoints.Add(castPoint);

                    Vector3 weaponDir = castPoint - weaponFirePort; // we should normalize this, however, setting the magnitude to 1f allows us to skip that
                    Vector3 eyeDir = castPoint - eyePosition;
                    // float eyeDirMag = partDistances[raycastData.PartType];

                    //raycastCommands[commands] = new RaycastCommand(eyePosition, eyeDir, eyeDirMag, _LOSMask);
                    raycastCommands[commands] = new RaycastCommand(eyePosition, eyeDir, new QueryParameters
                    {
                        layerMask = _LOSMask
                    }, 1f);
                    commands++;

                    //raycastCommands[commands] = new RaycastCommand(eyePosition, eyeDir, eyeDirMag, _VisionMask);
                    raycastCommands[commands] = new RaycastCommand(eyePosition, eyeDir, new QueryParameters
                    {
                        layerMask = _VisionMask
                    }, 1f);
                    commands++;

                    //raycastCommands[commands] = new RaycastCommand(weaponFirePort, weaponDir, weaponDir.magnitude, _ShootMask);
                    raycastCommands[commands] = new RaycastCommand(weaponFirePort, weaponDir, new QueryParameters
                    {
                        layerMask = _ShootMask
                    }, 1f);
                    commands++;
                }
            }
        }

        private void AnalyzeHits(List<Enemy> enemies, NativeArray<RaycastHit> raycastHits, int enemyCount, int partCount)
        {
            float time = Time.time;
            int hits = 0;
            int colliderTypeCount = 0;

            for (int i = 0; i < enemyCount; i++)
            {
                var enemy = _enemies[i];
                var visionChecker = enemy.Vision.VisionChecker;
                var parts = visionChecker.EnemyParts.PartsArray;
                visionChecker.LastCheckLOSTime = time + (enemy.IsAI ? 0.1f : 0.05f);
                enemy.Bot.Vision.TimeLastCheckedLOS = time;

                for (int j = 0; j < partCount; j++)
                {
                    var part = parts[j];
                    EBodyPartColliderType colliderType = _colliderTypes[colliderTypeCount];
                    Vector3 castPoint = _castPoints[colliderTypeCount];
                    colliderTypeCount++;

                    part.SetLineOfSight(castPoint, colliderType, raycastHits[hits], ERaycastCheck.LineofSight, time);
                    hits++;
                    part.SetLineOfSight(castPoint, colliderType, raycastHits[hits], ERaycastCheck.Vision, time);
                    hits++;
                    part.SetLineOfSight(castPoint, colliderType, raycastHits[hits], ERaycastCheck.Shoot, time);
                    hits++;
                }
            }
        }

        private const int RAYCAST_CHECKS = 3;
        private readonly LayerMask _LOSMask = LayerMaskClass.HighPolyWithTerrainMask;
        private readonly LayerMask _VisionMask = LayerMaskClass.AI;
        private readonly LayerMask _ShootMask = LayerMaskClass.HighPolyWithTerrainMask;
        private int _partCount = -1;
        private readonly List<EBodyPartColliderType> _colliderTypes = new();
        private readonly List<Vector3> _castPoints = new();

        private static void FindEnemies(BotDictionary bots, List<Enemy> result)
        {
            result.Clear();
            float time = Time.time;
            foreach (var bot in bots.Values)
            {
                if (bot == null || !bot.BotActive)
                {
                    continue;
                }

                if (bot.Vision.TimeSinceCheckedLOS < 0.05f)
                {
                    continue;
                }

                foreach (var enemy in bot.EnemyController.Enemies.Values)
                {
                    if (!enemy.CheckValid())
                    {
                        continue;
                    }

                    var visionChecker = enemy.Vision.VisionChecker;

                    if (enemy.RealDistance > visionChecker.AIVisionRangeLimit())
                    {
                        continue;
                    }

                    if (visionChecker.LastCheckLOSTime < time)
                    {
                        result.Add(enemy);
                    }
                }
            }
        }

        private readonly List<Enemy> _enemies = new();
    }
}