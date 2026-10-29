using UnityEngine;

namespace ArcherLegend.Infrastructure
{
    /// <summary>
    /// インフラ層の生成係です。アート素材が揃ったらプレハブや外部読み込みへ差し替えます。
    /// </summary>
    public sealed class UnityObjectFactory
    {
        private readonly Material playerMaterial;
        private readonly Material enemyMaterial;
        private readonly Material arrowMaterial;
        private readonly Material floorMaterial;
        private readonly Material wallMaterial;

        public UnityObjectFactory()
        {
            // 描画素材はここに集約し、表示側がレンダリング詳細を知らなくて済むようにします。
            playerMaterial = CreateMaterial("Hero Jade", new Color(0.10f, 0.68f, 0.52f));
            enemyMaterial = CreateMaterial("Enemy Coral", new Color(0.86f, 0.23f, 0.22f));
            arrowMaterial = CreateMaterial("Arrow Gold", new Color(1.00f, 0.78f, 0.25f));
            floorMaterial = CreateMaterial("Arena Stone", new Color(0.22f, 0.24f, 0.27f));
            wallMaterial = CreateMaterial("Arena Wall", new Color(0.39f, 0.42f, 0.46f));
        }

        public GameObject CreatePlayer()
        {
            // 図形オブジェクトは仮素材です。後からプレハブに置き換えても生成口は維持します。
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Archer";
            player.transform.localScale = new Vector3(0.8f, 1.0f, 0.8f);
            player.GetComponent<Renderer>().sharedMaterial = playerMaterial;
            AddKinematicBody(player);
            return player;
        }

        public GameObject CreateEnemy()
        {
            var enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemy.name = "Chaser";
            enemy.transform.localScale = new Vector3(0.85f, 0.9f, 0.85f);
            enemy.GetComponent<Renderer>().sharedMaterial = enemyMaterial;
            AddKinematicBody(enemy);
            return enemy;
        }

        public GameObject CreateArrow()
        {
            var arrow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            arrow.name = "Arrow";
            arrow.transform.localScale = new Vector3(0.08f, 0.38f, 0.08f);
            arrow.GetComponent<Renderer>().sharedMaterial = arrowMaterial;
            Object.Destroy(arrow.GetComponent<Collider>());
            return arrow;
        }

        public void CreateArena(float halfWidth, float halfDepth)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Arena Floor";
            floor.transform.position = new Vector3(0f, -0.08f, 0f);
            floor.transform.localScale = new Vector3(halfWidth * 2f, 0.1f, halfDepth * 2f);
            floor.GetComponent<Renderer>().sharedMaterial = floorMaterial;

            CreateWall("North Wall", new Vector3(0f, 0.5f, halfDepth + 0.35f), new Vector3(halfWidth * 2f + 1f, 1f, 0.7f));
            CreateWall("South Wall", new Vector3(0f, 0.5f, -halfDepth - 0.35f), new Vector3(halfWidth * 2f + 1f, 1f, 0.7f));
            CreateWall("East Wall", new Vector3(halfWidth + 0.35f, 0.5f, 0f), new Vector3(0.7f, 1f, halfDepth * 2f + 1f));
            CreateWall("West Wall", new Vector3(-halfWidth - 0.35f, 0.5f, 0f), new Vector3(0.7f, 1f, halfDepth * 2f + 1f));
        }

        private void CreateWall(string name, Vector3 position, Vector3 scale)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.position = position;
            wall.transform.localScale = scale;
            wall.GetComponent<Renderer>().sharedMaterial = wallMaterial;
        }

        private static void AddKinematicBody(GameObject target)
        {
            // 現状の移動は座標直接操作です。物理コンポーネントは将来の衝突拡張に備えた土台です。
            var body = target.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.isKinematic = true;
        }

        private static Material CreateMaterial(string name, Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.name = name;
            material.color = color;
            return material;
        }
    }
}
