using System.Collections.Generic;
using ArcherLegend.Application;
using ArcherLegend.Infrastructure;
using ArcherLegend.Presentation.Result;
using UnityEngine;

namespace ArcherLegend.Presentation.Game
{
    /// <summary>
    /// Unity 側の調整役です。入力・Transform・GameObject を Application 層の呼び出しへ変換します。
    /// </summary>
    public sealed class ArcherGameController : MonoBehaviour
    {
        private const float ArenaHalfWidth = 9f;
        private const float ArenaHalfDepth = 6f;

        private readonly List<EnemyView> enemies = new List<EnemyView>();
        private ArcherGameSession session;
        private UnityObjectFactory factory;
        private PlayerProgressService progress;
        private ISceneNavigator navigator;
        private GameObject player;
        private BlinkView playerBlink;
        private GameHudPresenter hudPresenter;
        private ResultPresenter resultPresenter;
        private float fireTimer;
        private int enemiesToSpawn;
        private bool isRunFinished;

        public void Construct(ArcherGameSession gameSession, UnityObjectFactory objectFactory, PlayerProgressService playerProgress, ISceneNavigator sceneNavigator)
        {
            session = gameSession;
            factory = objectFactory;
            progress = playerProgress;
            navigator = sceneNavigator;
        }

        private void Start()
        {
            // 構成が固まるまでは Runtime 生成にして、シーン上の手作業セットアップを減らします。
            factory.CreateArena(ArenaHalfWidth, ArenaHalfDepth);
            player = factory.CreatePlayer();
            player.transform.position = Vector3.up * 0.6f;
            playerBlink = player.AddComponent<BlinkView>();

            ArenaCamera.Create(ArenaHalfWidth, ArenaHalfDepth);

            var lightObject = new GameObject("Key Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.5f;
            light.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            var hudView = new GameObject("Archer HUD").AddComponent<GameHudView>();
            hudPresenter = new GameHudPresenter(hudView, session);
            PresenterLifetime.Bind(hudView.gameObject, hudPresenter);

            var resultView = new GameObject("Result").AddComponent<ResultView>();
            resultPresenter = new ResultPresenter(resultView, progress, navigator, Restart);
            PresenterLifetime.Bind(resultView.gameObject, resultPresenter);

            BeginWave();
        }

        private void Update()
        {
            // レベルアップの選択中とゲームオーバー後は、敵や矢を含めたゲーム内の時間を止める。
            // 敵と矢は自分の Update で Time.deltaTime を使って動くので、timeScale を 0 にすれば一括で止まる。
            // uGUI のボタンは時間停止の影響を受けないので、選択肢やリザルトは操作できる。
            Time.timeScale = session.IsUpgradePending || session.IsGameOver ? 0f : 1f;

            // 大きな状態判断は Domain / Application に任せ、このクラスは Unity 上の反応だけを決めます。
            if (session.IsGameOver)
            {
                if (!isRunFinished)
                {
                    isRunFinished = true;
                    playerBlink.SetBlinking(false);
                    _ = resultPresenter.ShowAsync(session.CreateRunResult());
                }

                hudPresenter.Refresh(enemies.Count);
                return;
            }

            if (session.IsUpgradePending)
            {
                hudPresenter.Refresh(enemies.Count);
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

            hudPresenter.Refresh(enemies.Count);
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

            // 被弾後の無敵時間中は敵に触れてもダメージを受けず、その間はプレイヤーを点滅させる
            session.Player.TickInvincibility(Time.deltaTime);
            foreach (var enemy in enemies)
            {
                if (enemy != null && enemy.IsTouchingTarget() && session.Player.TryTakeHit(enemy.Model.ContactDamage))
                {
                    break;
                }
            }

            playerBlink.SetBlinking(session.Player.IsInvincible);

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
            var arrow = factory.CreateArrow().AddComponent<ArrowView>();
            arrow.Launch(player.transform.position + Vector3.up * 0.25f + direction.normalized * 0.55f, direction, session.Player.ArrowDamage, 12f, OnEnemyDefeated);
            fireTimer = session.Player.FireCooldown;
        }

        private EnemyView FindNearestEnemy()
        {
            EnemyView nearest = null;
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
            var enemy = factory.CreateEnemy().AddComponent<EnemyView>();
            enemy.transform.position = position;
            enemy.Construct(session.CreateEnemy(), player.transform);
            enemies.Add(enemy);
        }

        private void OnEnemyDefeated(EnemyView enemy)
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

            foreach (var arrow in FindObjectsByType<ArrowView>(FindObjectsSortMode.None))
            {
                Destroy(arrow.gameObject);
            }

            enemies.Clear();
            session.Restart();
            isRunFinished = false;
            player.transform.position = Vector3.up * 0.6f;
            BeginWave();
        }

        private void OnDestroy()
        {
            // timeScale はシーンをまたいで残るので、ゲーム画面を抜けるときに必ず元に戻す
            Time.timeScale = 1f;
            session?.Dispose();
        }
    }
}
