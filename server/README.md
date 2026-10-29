# ArcherLegend サーバー

ゲーム用 API と、運営向けのアンケート作成・集計ツールです。

## 技術構成

- ASP.NET Core (.NET 10) / Blazor (Interactive Server)
- PostgreSQL 17 / Entity Framework Core
- xUnit（テスト）

## ローカルでの起動方法

必要なもの: .NET 10 SDK、Docker（以下のコマンドは `server/` で実行）

```bash
# データベースを起動
docker compose up -d

# EF Core ツールを復元し、マイグレーションを適用
dotnet tool restore
dotnet ef database update -p src/SurveyApp

# アプリを起動
dotnet run --project src/SurveyApp
```

開発環境では `http://localhost:5032/openapi/v1.json` で API の定義（OpenAPI）を確認できます。

## テスト

```bash
dotnet test
```

API のテストは Testcontainers で PostgreSQL のコンテナを立ち上げて実行するため、Docker が起動している必要があります。

## ゲーム用 API

ログインは行わず、ゲームが初回起動時に生成したプレイヤー Id（GUID）で識別します。
Id は本人確認も兼ねるため、ランキングなど他のプレイヤーに見える場所には含めません。

| メソッド | パス | 内容 |
| --- | --- | --- |
| POST | `/api/players` | プレイヤー登録。同じ Id の再送は登録済みのプレイヤーを返す |
| GET | `/api/players/{id}` | プレイヤーと戦績の取得 |
| PUT | `/api/players/{id}/name` | 名前の変更（1〜20 文字） |
| POST | `/api/players/{id}/runs` | プレイ結果の送信。戦績を更新し、自己ベスト更新の有無を返す |
| GET | `/api/rankings?playerId=&limit=` | スコアランキング（同点は先に達成した順）。`playerId` を渡すと自分の順位も返す |

### 設計で気をつけたこと

- **通信の再送に強くする**：プレイ結果にはゲーム側で生成した `runId` を付け、同じ結果が再送されても二重に記録しない（最初と同じ結果を返す）。プレイヤー登録も同じ Id なら何度送っても 1 人として扱う
- **同時更新で戦績が消えないようにする**：PostgreSQL の `xmin` を使った楽観的同時実行制御で、同じプレイヤーの結果が同時に届いたときは 409 を返して送り直してもらう
- **戦績はサーバーを正とする**：自己ベストや累計はサーバー側で計算し、1 回ごとの結果も `Runs` テーブルに残して運営ツールでの分析に使えるようにする
- **今回の範囲外**：スコアの改ざん対策（リプレイ検証など）や、Id の漏えい対策（端末ごとの秘密トークンなど）は扱っていない

## 機能（予定）

- [x] ゲーム用 API（プレイヤー登録・戦績・ランキング）
- [ ] 運営ツールでのアンケート作成（単一選択・複数選択・自由記述）
- [ ] ゲーム内でのアンケート回答
- [ ] 回答の集計とグラフ表示・CSV 出力
