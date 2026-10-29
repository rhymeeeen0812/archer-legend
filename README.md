# SurveyApp

アンケートを作成・公開し、回答を集計できる Web アプリです。

## 技術構成

- ASP.NET Core (.NET 10) / Blazor (Interactive Server)
- PostgreSQL 17 / Entity Framework Core
- ASP.NET Core Identity（ログイン認証）
- xUnit（テスト）

## ローカルでの起動方法

必要なもの: .NET 10 SDK、Docker

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

- [ ] アンケート作成（単一選択・複数選択・自由記述）
- [ ] 公開 URL での回答
- [ ] 結果の集計とグラフ表示
- [ ] 回答期限・重複回答の防止
- [ ] CSV 出力
- [ ] 条件分岐
