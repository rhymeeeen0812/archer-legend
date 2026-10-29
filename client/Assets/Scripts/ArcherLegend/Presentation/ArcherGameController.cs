using System.Collections.Generic;
using ArcherLegend.Application;
using ArcherLegend.Infrastructure;
using UnityEngine;

namespace ArcherLegend.Presentation
{
    /// <summary>
    /// Unity 側の調整役です。入力・Transform・GameObject を Application 層の呼び出しへ変換します。
    /// </summary>
    public sealed class ArcherGameController : MonoBehaviour
    {
        private const float ArenaHalfWidth = 9f;
        private const float ArenaHalfDepth = 6f;

        private readonly List<EnemyPresenter> enemies = new List<EnemyPresenter>();
        private ArcherGameSession session;
        private UnityObjectFactory factory;
        private GameObject player;
        private GameHudPresenter hud;
        private float fireTimer;
        private int enemiesToSpawn;

        public void Construct(ArcherGameSession gameSession, UnityObjectFactory objectFactory)
        {
            session = gameSession;
            factory = objectFactory;
        }

        private void Start()
        {
            // 構成が固まるまでは Runtime 生成にして、シーン上の手作業セットアップを減らします。
            factory.CreateArena(ArenaHalfWidth, ArenaHalfDepth);
            player = factory.CreatePlayer();
            player.transform.position = Vector3.up * 0.6f;

            foreach (var existingCamera in FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                existingCamera.enabled = false;
            }

            var cameraObject = new GameObject("Top Down Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 13.5f, -2.5f);
            camera.transform.rotation = Quaternion.Euler(78f, 0f, 0f);
            camera.orthographic = true;
            camera.orthographicSize = 8.4f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.10f, 0.12f, 0.15f);

            var lightObject = new GameObject("Key Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.5f;
            light.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            hud = new GameObject("Archer HUD").AddComponent<GameHudPresenter>();
            hud.Construct(type => session.ApplyUpgrade(type), Restart);

            BeginWave();
        }

        private void Update()
        {
            // 大きな状態判断は Domain / Application に任せ、このクラスは Unity 上の反応だけを決めます。
            if (session.IsGameOver)
            {
                if (Input.GetKeyDown(KeyCode.R))
                {
                    Restart();
                }

                hud.Render(session, enemies.Count);
                return;
            }

            if (session.IsUpgradePending)
            {
                hud.Render(session, enemies.Count);
                return;
            }

            MovePlayer();
            UpdateCombat();
            CullMissingEnemies();

            if (enemies.Count == 0 && enemiesToSpawn <= 0)
            {
                session.RegisterWaveCleared();
                BeginWave();
            }

            hud.Render(session, enemies.Count);
        }

        private void MovePlayer()
        {
            var input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            if (input.sqrMagnitude > 1f)
            {
                input.Normalize();
            }

            player.transform.position += input * session.Player.MoveSpeed * Time.deltaTime;
            player.transform.position = new Vector3(
                Mathf.Clamp(player.transform.position.x, -ArenaHalfWidth + 0.6f, ArenaHalfWidth - 0.6f),
                0.6f,
                Mathf.Clamp(player.transform.position.z, -ArenaHalfDepth + 0.6f, ArenaHalfDepth - 0.6f));

            if (input.sqrMagnitude > 0.01f)
            {
                player.transform.forward = input.normalized;
            }
        }

        private void UpdateCombat()
        {
            fireTimer -= Time.deltaTime;

            foreach (var enemy in enemies)
            {
                if (enemy != null && enemy.TryDealContactDamage(Time.deltaTime, out var damage))
                {
                    session.Player.TakeDamage(damage);
                }
            }

            var movement = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (movement.sqrMagnitude > 0.01f || fireTimer > 0f)
            {
                return;
            }

            var nearest = FindNearestEnemy();
            if (nearest == null)
            {
                return;
            }

            var direction = nearest.transform.position - player.transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f)
            {
                return;
            }

            player.transform.forward = direction.normalized;
            var arrow = factory.CreateArrow().AddComponent<ArrowPresenter>();
            arrow.Launch(player.transform.position + Vector3.up * 0.25f + direction.normalized * 0.55f, direction, session.Player.ArrowDamage, 12f, OnEnemyDefeated);
            fireTimer = session.Player.FireCooldown;
        }

        private EnemyPresenter FindNearestEnemy()
        {
            EnemyPresenter nearest = null;
            var nearestDistance = float.MaxValue;
            foreach (var enemy in enemies)
            {
                if (enemy == null)
                {
                    continue;
                }

                var distance = (enemy.transform.position - player.transform.position).sqrMagnitude;
                if (distance >= nearestDistance)
                {
                    continue;
                }

                nearestDistance = distance;
                nearest = enemy;
            }

            return nearest;
        }

        private void BeginWave()
        {
            enemiesToSpawn = 3 + session.Wave;
            for (var i = 0; i < enemiesToSpawn; i++)
            {
                SpawnEnemy(i);
            }

            enemiesToSpawn = 0;
        }

        private void SpawnEnemy(int index)
        {
            var angle = index * Mathf.PI * 2f / Mathf.Max(1, enemiesToSpawn);
            var position = new Vector3(Mathf.Cos(angle) * (ArenaHalfWidth - 1.4f), 0.55f, Mathf.Sin(angle) * (ArenaHalfDepth - 1.4f));
            var enemy = factory.CreateEnemy().AddComponent<EnemyPresenter>();
            enemy.transform.position = position;
            enemy.Construct(session.CreateEnemy(), player.transform);
            enemies.Add(enemy);
        }

        private void OnEnemyDefeated(EnemyPresenter enemy)
        {
            session.RegisterEnemyDefeated(enemy.Model);
            enemies.Remove(enemy);
        }

        private void CullMissingEnemies()
        {
            for (var i = enemies.Count - 1; i >= 0; i--)
            {
                if (enemies[i] == null)
                {
                    enemies.RemoveAt(i);
                }
            }
        }

        private void Restart()
        {
            foreach (var enemy in enemies)
            {
                if (enemy != null)
                {
                    Destroy(enemy.gameObject);
                }
            }

            foreach (var arrow in FindObjectsByType<ArrowPresenter>(FindObjectsSortMode.None))
            {
                Destroy(arrow.gameObject);
            }

            enemies.Clear();
            session.Restart();
            player.transform.position = Vector3.up * 0.6f;
            BeginWave();
        }

        private void OnDestroy()
        {
            session?.Dispose();
        }
    }
}
