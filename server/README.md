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

## テスト

```bash
dotnet test
```

## 機能（予定）

- [ ] ゲーム用 API（プレイヤー登録・セーブデータ・ランキング）
- [ ] 運営ツールでのアンケート作成（単一選択・複数選択・自由記述）
- [ ] ゲーム内でのアンケート回答
- [ ] 回答の集計とグラフ表示・CSV 出力
