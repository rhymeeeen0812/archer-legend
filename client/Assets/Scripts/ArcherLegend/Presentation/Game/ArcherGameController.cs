using System.Collections.Generic;
using ArcherLegend.Application;
using ArcherLegend.Domain;
using ArcherLegend.Infrastructure;
using ArcherLegend.Presentation.Result;
using UnityEngine;

namespace ArcherLegend.Presentation.Game
{
    /// <summary>
    /// Unity 側の調整役です。入力・Transform・GameObject を Application 層の呼び出しへ変換します。
    /// ステージ（部屋）の流れ: ウェーブを順に倒す → 最後のウェーブを倒すと出口が開く → 出口に入ると次の部屋へ移る。
    /// </summary>
    public sealed class ArcherGameController : MonoBehaviour
    {
        private const float ArenaHalfWidth = StageLayouts.ArenaHalfWidth;
        private const float ArenaHalfDepth = StageLayouts.ArenaHalfDepth;
        private const float SafeSpawnDistance = 3f;
        private const float ArrowSpeed = 12f;

        // 障害物とぶつかるときのプレイヤーの体の半径
        private const float PlayerRadius = 0.45f;

        /// <summary>拡散矢で隣り合う矢の間の角度（度）です。</summary>
        private const float SpreadAngle = 15f;

        private readonly List<EnemyView> enemies = new List<EnemyView>();

        // 今のステージの障害物。敵・矢・弾はこの一覧を参照するので、ステージが変わっても同じインスタンスの中身を入れ替える
        private readonly List<Rect> obstacles = new List<Rect>();
        private readonly List<GameObject> obstacleObjects = new List<GameObject>();

        private ArcherGameSession session;
        private UnityObjectFactory factory;
        private PlayerProgressService progress;
        private OnlineSyncService sync;
        private ISceneNavigator navigator;
        private PlayerInputReader input;
        private GameObject player;
        private BlinkView playerBlink;
        private GameHudPresenter hudPresenter;
        private ResultPresenter resultPresenter;
        private GameObject exit;
        private float fireTimer;
        private bool isRunFinished;

        private bool IsExitOpen => exit != null;

        public void Construct(ArcherGameSession gameSession, UnityObjectFactory objectFactory, PlayerProgressService playerProgress, OnlineSyncService onlineSync, ISceneNavigator sceneNavigator)
        {
            session = gameSession;
            factory = objectFactory;
            progress = playerProgress;
            sync = onlineSync;
            navigator = sceneNavigator;
        }

        private void Start()
        {
            // 構成が固まるまでは Runtime 生成にして、シーン上の手作業セットアップを減らします。
            input = new PlayerInputReader();
            factory.CreateArena(ArenaHalfWidth, ArenaHalfDepth);
            player = factory.CreatePlayer();
            playerBlink = player.AddComponent<BlinkView>();

            ArenaCamera.Create(ArenaHalfWidth, ArenaHalfDepth);

            var lightObject = new GameObject("Key Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.5f;
            light.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            var hudView = ViewPrefabs.Instantiate<GameHudView>();
            hudPresenter = new GameHudPresenter(hudView, session);
            PresenterLifetime.Bind(hudView.gameObject, hudPresenter);

            var resultView = ViewPrefabs.Instantiate<ResultView>();
            resultPresenter = new ResultPresenter(resultView, progress, sync, navigator, Restart);
            PresenterLifetime.Bind(resultView.gameObject, resultPresenter);

            EnterStage();
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

                hudPresenter.Refresh(enemies.Count, IsExitOpen);
                return;
            }

            if (session.IsUpgradePending)
            {
                hudPresenter.Refresh(enemies.Count, IsExitOpen);
                return;
            }

            var move = input.Move;
            MovePlayer(move);
            UpdateCombat(move);
            CullMissingEnemies();

            if (IsExitOpen)
            {
                if (StageLayouts.IsInExit(player.transform.position))
                {
                    AdvanceStage();
                }
            }
            else if (enemies.Count == 0)
            {
                if (session.IsLastWaveOfStage)
                {
                    // ステージの最後のウェーブを倒しきったら、すぐ次へは進まず出口を開いて待つ
                    exit = factory.CreateExit(StageLayouts.ExitZone);
                }
                else
                {
                    session.RegisterWaveCleared();
                    BeginWave();
                }
            }

            hudPresenter.Refresh(enemies.Count, IsExitOpen);
        }

        private void MovePlayer(Vector2 move)
        {
            var direction = new Vector3(move.x, 0f, move.y);
            var next = player.transform.position + direction * session.Player.MoveSpeed * Time.deltaTime;
            next = new Vector3(
                Mathf.Clamp(next.x, -ArenaHalfWidth + 0.6f, ArenaHalfWidth - 0.6f),
                0.6f,
                Mathf.Clamp(next.z, -ArenaHalfDepth + 0.6f, ArenaHalfDepth - 0.6f));
            player.transform.position = ArenaGeometry.PushOut(next, PlayerRadius, obstacles);

            if (direction.sqrMagnitude > 0.01f)
            {
                player.transform.forward = direction.normalized;
            }
        }

        private void UpdateCombat(Vector2 move)
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

            // 立ち止まっている間だけ自動で矢を放つ
            if (move.sqrMagnitude > 0.01f || fireTimer > 0f)
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
            var stats = session.Player;
            var shot = new ArrowShot(stats.ArrowDamage, ArrowSpeed, stats.PierceCount, stats.BounceCount, ArenaHalfWidth, ArenaHalfDepth, obstacles);
            foreach (var arrowDirection in ArrowView.SpreadDirections(direction, stats.ArrowCount, SpreadAngle))
            {
                var arrow = factory.CreateArrow().AddComponent<ArrowView>();
                arrow.Launch(player.transform.position + Vector3.up * 0.25f + arrowDirection * 0.55f, arrowDirection, shot, enemies, OnEnemyDefeated);
            }

            fireTimer = stats.FireCooldown;
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

        /// <summary>出口に入ったら、通算のウェーブを 1 つ進めて次の部屋へ移ります。</summary>
        private void AdvanceStage()
        {
            session.RegisterWaveCleared();
            EnterStage();
        }

        /// <summary>今のステージの部屋を作り直し、プレイヤーを入口に置いて最初のウェーブを始めます。</summary>
        private void EnterStage()
        {
            ClearProjectiles();
            CloseExit();

            foreach (var obstacleObject in obstacleObjects)
            {
                Destroy(obstacleObject);
            }

            obstacleObjects.Clear();
            obstacles.Clear();
            obstacles.AddRange(StageLayouts.For(session.Stage));
            foreach (var area in obstacles)
            {
                obstacleObjects.Add(factory.CreateObstacle(area));
            }

            player.transform.position = StageLayouts.PlayerStart;
            player.transform.forward = Vector3.forward;
            BeginWave();
        }

        private void BeginWave()
        {
            var types = session.ComposeWave();
            for (var i = 0; i < types.Length; i++)
            {
                SpawnEnemy(types[i], i, types.Length);
            }
        }

        /// <summary>プレイヤーの近くに出現する敵は、避ける間もなく当たらないようアリーナの反対側へ移します。</summary>
        public static Vector3 ResolveSpawnPosition(Vector3 candidate, Vector3 playerPosition)
        {
            var offset = candidate - playerPosition;
            offset.y = 0f;
            return offset.sqrMagnitude < SafeSpawnDistance * SafeSpawnDistance
                ? new Vector3(-candidate.x, candidate.y, -candidate.z)
                : candidate;
        }

        private void SpawnEnemy(EnemyType type, int index, int count)
        {
            var angle = index * Mathf.PI * 2f / count;
            var position = new Vector3(Mathf.Cos(angle) * (ArenaHalfWidth - 1.4f), 0.55f, Mathf.Sin(angle) * (ArenaHalfDepth - 1.4f));
            position = ResolveSpawnPosition(position, player.transform.position);

            var enemy = factory.CreateEnemy(type).AddComponent<EnemyView>();
            enemy.transform.position = ArenaGeometry.PushOut(position, PlayerRadius, obstacles);
            enemy.Construct(session.CreateEnemy(type), player.transform, ArenaHalfWidth, ArenaHalfDepth, obstacles, OnEnemyShoot);
            enemies.Add(enemy);
        }

        private void OnEnemyShoot(EnemyView shooter, Vector3 direction)
        {
            var bullet = factory.CreateEnemyBullet().AddComponent<EnemyBulletView>();
            var start = shooter.transform.position + Vector3.up * 0.25f + direction * 0.6f;
            bullet.Launch(start, direction, shooter.Model.ShotDamage, player.transform, OnPlayerShot, ArenaHalfWidth, ArenaHalfDepth, obstacles);
        }

        private void OnPlayerShot(int damage)
        {
            // 体当たりと同じく、無敵時間中は当たってもダメージを受けない
            session.Player.TryTakeHit(damage);
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

        private void ClearProjectiles()
        {
            foreach (var arrow in FindObjectsByType<ArrowView>(FindObjectsSortMode.None))
            {
                Destroy(arrow.gameObject);
            }

            foreach (var bullet in FindObjectsByType<EnemyBulletView>(FindObjectsSortMode.None))
            {
                Destroy(bullet.gameObject);
            }
        }

        private void CloseExit()
        {
            if (exit != null)
            {
                Destroy(exit);
                exit = null;
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

            enemies.Clear();
            session.Restart();
            isRunFinished = false;
            EnterStage();
        }

        private void OnDestroy()
        {
            // timeScale はシーンをまたいで残るので、ゲーム画面を抜けるときに必ず元に戻す
            Time.timeScale = 1f;
            session?.Dispose();
        }
    }
}
