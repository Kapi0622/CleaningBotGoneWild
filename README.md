# お掃除ロボ、暴走中。
### Cleaning Bot Gone Wild

> ゴミを消すためなら、部屋が吹き飛んでも構わない

![Unity](https://img.shields.io/badge/Unity-6000.4.4f1-black?logo=unity)
![Language](https://img.shields.io/badge/言語-C%23-239120?logo=csharp)
![Platform](https://img.shields.io/badge/Platform-Windows-0078d4?logo=windows)
![Status](https://img.shields.io/badge/Status-開発中-yellow)

> 📸 ゲームプレイ画像・GIF 準備中

---

## ゲーム概要

プレイヤーは暴走した最新型 AI お掃除ロボットを操作し、**制限時間内に部屋のゴミをすべて除去**するステージクリア型 3D アクションゲームです。

ロケットやブラックホールカプセルを使えばゴミは一掃できますが、床や住人への被害は免れません。  
「掃除するはずなのに部屋を壊す」という逆説がゲームの核心です。

| 項目 | 内容 |
|------|------|
| ジャンル | ハチャメチャ掃除アクション（ステージクリア型） |
| 視点 | 3D 固定俯瞰カメラ（オーバークック式） |
| プラットフォーム | PC（Windows）・ゲームパッド両対応 |
| バージョン | 0.1.0（開発中） |

---

## ゲームシステム

### 基本ルール

| 項目 | 内容 |
|------|------|
| クリア条件 | 制限時間内に部屋のゴミをすべて除去する |
| 失敗条件① | 制限時間切れ（ゴミが残っている） |
| 失敗条件② | 床が崩壊してロボが落下する |

### 武器と副作用

| 武器 | 特徴 | 副作用 | クールタイム |
|------|------|--------|------------|
| 🧹 掃除機 | 唯一副作用ゼロの安全手段 | なし（住人に向けると怒る） | なし |
| 🚀 ロケット | 爽快感・高破壊力 | 床ダメージ・住人を吹き飛ばす | 3〜4 秒 |
| ⚫ ブラックホール | 範囲一掃・ハイリスク | 床に大ダメージ・住人も吸い込む | 8〜10 秒 |

すべてのゴミはどの武器でも除去できます。武器の使い分けはクールタイムと副作用によって自然に促される設計です。

### スコアシステム

```
メインスコア ＝ 除去したゴミ数 × 残り時間ボーナス
```

全てのゴミを消去すると、ボーナスフェーズに突入。
時間が尽きるまでスコアを稼ぎまくれ！

リザルト画面には「被害総額」（破壊した家具・住人への損害賠償）も表示されます。

### ランクシステム

| ランク | 取得条件 |
|--------|----------|
| ★★★ | クリア ＋ メインスコアが規定値以上（高難易度） |
| ★★☆ | クリア ＋ メインスコアが規定値以上 |
| ★☆☆ | クリア（ゴミをすべて除去） |

---

## 操作方法

| 入力 | アクション |
|------|-----------|
| `WASD` | 移動 |
| `Space` | ジャンプ |
| `1` / `2` / `3` | 武器切替（掃除機 / ロケット / ブラックホール） |
| 左クリック | 攻撃・発射 |

---

## 技術スタック

| レイヤー | 採用技術 | 選定理由 |
|---------|---------|---------|
| エンジン | Unity 6 (6000.4.4f1) | 3D アクションのプロトタイピング速度が最速 |
| 言語 | C# | 型安全性と LINQ が効く Unity 標準言語 |
| 非同期 | UniTask 2.5.10 | 武器演出が必要とする CancellationToken 対応の非同期シーケンス |
| リアクティブ | R3（Cysharp） | ReactiveProperty / Subject で MVP 通知層を構築。UniRx の後継 |
| カメラ | Cinemachine 3.1.6 | 部屋ごとの VirtualCamera 切り替えを宣言的に記述できる |
| トゥイーン | LitMotion | ゼロアロケーション指向で R3 との親和性が高い |
| 入力 | Unity Input System | キーボード・マウス・ゲームパッド統一管理 |

**あえて使わなかった技術：**

| 技術 | 不採用の理由 |
|------|------------|
| DIコンテナ（VContainer / Zenject） | `Startup.cs` 1ファイルの手動 DI で依存関係を透明に管理できる規模のため |
| `static` / シングルトン | R3 の Observable による通知で代替。グローバル状態を排除 |
| DOTween | LitMotion に移行（STEP 15）。ライセンスと API 設計で優位と判断 |

---

## アーキテクチャ

### 採用パターン

| パターン | 適用箇所 |
|---------|---------|
| MVP | スコア・タイマー・武器・ゴミ数・リザルトの UI |
| Strategy | 武器システム（`IWeaponStrategy`） |
| State | ゲーム状態管理（`GameStateController`） |
| 手動 DI | `Startup.cs` による依存注入 |

### Scripts フォルダ構成

```
Assets/_Project/Scripts/
├── Core/           # 状態管理・初期化（Startup、GameStateController、State群）
├── Stage/          # ステージ管理（StageLoader、StageInitializer）
├── Player/         # プレイヤー・武器（Locomotion、WeaponController、Weapons/）
├── Garbage/        # ゴミ（GarbageBase、NormalGarbage、GarbageRegistry）
├── Environment/    # 床・部屋（FloorGrid、FloorTile、RoomBounds）
├── Resident/       # 住人 AI（ResidentMover、ResidentReactor）
├── Score/          # MVP - Model 層（ScoreModel、TimerModel、RankCalculator）
├── Presenter/      # MVP - Presenter 層（Subscribe + AddTo のみ）
├── View/           # MVP - View 層（MonoBehaviour 継承）
├── Data/           # ScriptableObject 定義（StageData、WeaponData、GarbageData）
├── Camera/         # CameraDirector
├── Audio/          # BgmPlayer、UiSoundPlayer
└── Effects/        # ParticlePlayer
```

### 依存関係の方針

- `Startup.cs` 1ファイルで全依存を注入し、依存関係を一箇所に集約
- R3 通知フロー：`GarbageBase` → `GarbageRegistry` → `GarbageModel` → UI
- ScriptableObject（`StageData` / `WeaponData` / `GarbageData`）駆動でコード変更なしにステージ・武器・ゴミを追加可能

---

## 開発進捗

| STEP | 内容 | 状態 |
|------|------|------|
| 1〜2 | FloorGrid・PlayerLocomotion・Cinemachine 定点カメラ | ✅ 完了 |
| 3〜4 | 掃除機・ロケット・ゴミ除去判定 | ✅ 完了 |
| 5〜6 | 床崩壊システム・住人 AI | ✅ 完了 |
| 7 | ブラックホールカプセル（Strategy パターン完成） | ✅ 完了 |
| 8〜9 | MVP 構築・R3 通知フロー・GameStateController | ✅ 完了 |
| 10〜11 | StageResetter・StageData ScriptableObject 化 | ✅ 完了 |
| 12〜15 | エフェクト・カメラ演出・SE・UI アニメーション | ✅ 完了 |
| 16 | スコアシステム根本改修・ゴミ HP 制導入・複数部屋対応 | ✅ 完了 |
| 17 | タイトル画面・ステージ選択（3シーン構成確立） | ✅ 完了 |
| **18** | **複数ステージ追加・バランス調整** | 🔄 進行中 |

---

## ドキュメント

```
docs/
├── 企画書v3.md          # ゲームコンセプト・ルール・武器設計
├── 仕様書v2.md          # 技術仕様・クラス設計・依存関係
├── CLAUDE.md            # 設計ルール・禁止パターン・コードレビュー基準
├── project_summary/     # 設計詳細ドキュメント（9ファイル）
│   ├── 01_overview.md   # プロジェクト概要・技術選定理由
│   ├── 02_architecture.md
│   ├── 04_weapon_system.md
│   ├── 06_mvp_reactive.md
│   └── 09_retrospective.md  # スコア設計の失敗と学び
└── devlog/              # 各 STEP 完了レポート（STEP 2〜18）
```

---

## セットアップ

### 動作要件

- Unity **6000.4.4f1**（Unity Hub からインストール）
- Windows 10 / 11

### 手順

```bash
git clone https://github.com/Kapi0622/CleaningBotGoneWild.git
```

1. Unity Hub を開き **「Open」** → クローンした `Cleaning-Bot-Gone-Wild/Cleaning-Bot-Gone-Wild/` フォルダを選択
2. Unity 6 (6000.4.4f1) でプロジェクトを開く（初回はパッケージ解決に数分かかります）
3. `Assets/_Project/Scenes/TitleScene` を開いてプレイモードで起動

---

## ライセンス

本プロジェクトは個人開発・学習目的で制作しています。  
使用アセット（Kenney、JMO Assets 等）は各ライセンスに従います。
