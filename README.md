# ArcherLegend

見下ろし視点のウェーブ制アクションゲームと、そのゲームサーバー・運営ツールです。

| フォルダ | 内容 | 技術 |
|---|---|---|
| [`client/`](client/) | ゲーム本体 | Unity 6 (6000.0.37f1) / URP |
| [`server/`](server/) | ゲーム用 API・運営ツール（アンケート作成と集計） | ASP.NET Core (.NET 10) / Blazor / PostgreSQL |

## 構成

```
[Unity クライアント] ──HTTP──> [ASP.NET Core サーバー] ──> [PostgreSQL]
                                    └─ 運営ツール (Blazor)
```

ログインは行わず、ゲームが初回起動時に作ったプレイヤー ID で識別します。

## 起動方法

- ゲーム: Unity Hub で `client/` を開く
- サーバー: [`server/README.md`](server/README.md) を参照
