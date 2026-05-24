# タイピングバトルシステム 実装概要

## 実装したファイル

### 1. SkillData.cs
技のデータクラス
- 日本語名と英語名を保持
- 技の種類（攻撃、防御、回避、魔法）
- ダメージ倍率
- 言語に応じた名前の取得
- ダメージ計算（文字数 × 10 × 倍率）

### 2. TypingCombatSystem.cs
タイピング戦闘システムのコアロジック
- シングルトンパターンで実装
- 入力処理（文字入力、Backspace、Enter）
- 技の登録と管理
- 技名のマッチング（大文字小文字を区別しない）
- イベント通知（成功、失敗、入力変更）

### 3. TypingUI.cs
タイピングUIの表示管理
- 入力中のテキスト表示
- 使える技のリスト表示
- フィードバックメッセージ表示（成功/失敗）
- 効果音再生（オプション）

### 4. Bandit.cs（更新）
キャラクターコントローラーの更新
- TypingCombatSystemとの連携
- 技の種類に応じたアニメーション再生
- Escキーでポーズ機能追加
- 旧来のマウス攻撃操作を削除

### 5. EnemyHealth.cs
敵のHP管理システム
- 攻撃技によるダメージ処理
- HP管理と死亡判定
- TypingCombatSystemのイベント購読

## 主な機能

### 1. 言語対応
- LangManagerと連動
- 日本語/英語で異なる技名
- 言語切り替えで自動的に技名が変わる

### 2. ダメージ計算
```
ダメージ = 文字数 × 10 × 技の倍率
```
- 短い技名 → 低ダメージ、入力が簡単
- 長い技名 → 高ダメージ、入力が難しい

### 3. 技の種類
- **Attack（攻撃）**: 敵にダメージ、攻撃アニメーション
- **Defend（防御）**: 防御態勢（combat idle）
- **Dodge（回避）**: ジャンプで回避
- **Magic（魔法）**: 魔法攻撃（今後拡張予定）

### 4. イベントシステム
- `SkillExecuted`: 技が成功時に発火
- `InputFailed`: 入力ミス時に発火
- `InputChanged`: 入力が変更された時に発火

## 使い方

### Unity エディタでの設定

1. **TypingCombatSystemの設定**
   - 空のGameObjectを作成
   - TypingCombatSystemコンポーネントを追加

2. **UIの設定**
   - Canvasに以下を作成：
     - Input Display (TMP_Text): 入力表示
     - Skill List Display (TMP_Text): 技リスト
     - Feedback Display (TMP_Text): フィードバック
   - TypingUIコンポーネントをCanvasに追加
   - 各TextMeshProオブジェクトをアサイン

3. **敵の設定**
   - 敵オブジェクトにEnemyHealthコンポーネントを追加
   - Max Healthを設定

### ゲームプレイ
1. 技名をキーボードで入力（例: 「斬る」「slash」）
2. Enterキーで発動
3. 正しい技名 → 技が発動、ダメージ表示
4. 間違った技名 → エラーメッセージ、効果音
5. Escキー → ポーズ/再開

## カスタマイズ例

### 新しい技を追加
TypingCombatSystem.csのInitializeSkills()内に追加：

```csharp
availableSkills.Add(new SkillData
{
    japaneseName = "炎の魔法",
    englishName = "fire magic",
    skillType = SkillType.Magic,
    damageMultiplier = 2.0f
});
```

### ゲーム進行で技を解禁
```csharp
SkillData fireballSkill = new SkillData
{
    japaneseName = "火球",
    englishName = "fireball",
    skillType = SkillType.Magic,
    damageMultiplier = 1.8f
};
TypingCombatSystem.Instance.AddSkill(fireballSkill);
```

### ダメージ計算式の変更
SkillData.csのGetDamage()メソッドを編集：

```csharp
public int GetDamage()
{
    string name = GetLocalizedName();
    int characterCount = name.Length;
    // 難易度に応じた倍率を追加
    float difficultyMultiplier = 1.0f;
    switch (GameSettings.CurrentDifficulty)
    {
        case GameSettings.Difficulty.Easy:
            difficultyMultiplier = 1.5f;
            break;
        case GameSettings.Difficulty.Hard:
            difficultyMultiplier = 0.8f;
            break;
    }
    return (int)(characterCount * 10 * damageMultiplier * difficultyMultiplier);
}
```

## 今後の拡張案

### 1. コンボシステム
- 連続で技を決めるとコンボ倍率アップ
- 一定時間内に複数の技を入力

### 2. タイピング精度の評価
- 入力速度を測定
- ミスタイプ回数のカウント
- パーフェクト入力でボーナス

### 3. 魔法システムの拡張
- 属性を追加（火、水、雷など）
- 詠唱時間の概念
- MPコストの導入

### 4. 難易度連動
- 難易度によって使える技が変わる
- Hardでは複雑な技名のみ

### 5. UI強化
- 技のアイコン表示
- 入力候補の表示（オートコンプリート）
- ダメージ数値の派手な演出

## 注意点

- LangManagerがシーンに存在する必要があります
- GameObjectに適切にコンポーネントがアタッチされているか確認してください
- TextMeshPro (TMP)を使用しています
- Animatorに"Attack", "Jump"などのトリガーが設定されている必要があります
