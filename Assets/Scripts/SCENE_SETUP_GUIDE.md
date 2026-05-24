# クイックスタートガイド - タイピング戦闘システム

## ?? 5分でセットアップ！

### ステップ 1: システムオブジェクトの作成

1. **Hierarchy** で右クリック → **Create Empty**
2. 名前を `TypingCombatSystem` に変更
3. **TypingCombatSystem** コンポーネントをアタッチ

? これで戦闘システムの本体が完成！

---

### ステップ 2: UI の作成

#### 2-1. Canvas の作成
1. **Hierarchy** で右クリック → **UI → Canvas**
2. Canvas を選択し、**Canvas Scaler** の設定:
   - **UI Scale Mode**: Scale With Screen Size
   - **Reference Resolution**: 1920 x 1080

#### 2-2. タイピングパネルの作成
1. **Canvas** を右クリック → **UI → Panel**
2. 名前を `TypingPanel` に変更
3. **RectTransform** の設定:
   - **Anchor Presets**: Bottom (Alt を押しながらクリックで位置も設定)
   - **Width**: 800
   - **Height**: 400
   - **Pos Y**: 200

#### 2-3. 入力フィールドの作成 ? 重要！
1. **TypingPanel** を右クリック → **UI → Input Field - TextMeshPro**
2. 名前を `InputField` に変更
3. **RectTransform** の設定:
   - **Anchor**: Top Center
   - **Width**: 700
   - **Height**: 80
   - **Pos Y**: -60
4. **TMP Input Field** コンポーネントの設定:
   - **Text Component**: 自動設定される子オブジェクトの Text
   - **Placeholder**: "技名を入力してEnter..."
   - **Character Limit**: 20
   - **Content Type**: Standard
   - **Line Type**: Single Line
5. **Text** の設定（InputField の子オブジェクト）:
   - **Font Size**: 48
   - **Alignment**: Center
   - **Color**: 白

#### 2-4. 技リストの作成
1. **TypingPanel** を右クリック → **UI → Text - TextMeshPro**
2. 名前を `SkillListDisplay` に変更
3. **RectTransform** の設定:
   - **Anchor**: Middle Left
   - **Width**: 350
   - **Height**: 250
   - **Pos X**: 200
   - **Pos Y**: -40
4. **TextMeshPro** の設定:
   - **Font Size**: 20
   - **Alignment**: Top Left
   - **Color**: 黄色 (#FFFF00)

#### 2-5. フィードバック表示の作成
1. **TypingPanel** を右クリック → **UI → Text - TextMeshPro**
2. 名前を `FeedbackDisplay` に変更
3. **RectTransform** の設定:
   - **Anchor**: Top Center
   - **Width**: 700
   - **Height**: 60
   - **Pos Y**: -150
4. **TextMeshPro** の設定:
   - **Font Size**: 36
   - **Alignment**: Center
   - **Color**: 緑 (#00FF00)

---

### ステップ 3: TypingUI の設定

1. **Canvas**（または TypingPanel）を選択
2. **Add Component** → **TypingUI** を追加
3. インスペクターで以下を設定:

| 項目 | 設定するオブジェクト |
|------|---------------------|
| **Input Field** | InputField をドラッグ ? 必須 |
| **Skill List Display** | SkillListDisplay をドラッグ |
| **Feedback Display** | FeedbackDisplay をドラッグ |
| **Typing Panel** | TypingPanel をドラッグ（オプション） |

? これで UI の設定が完成！

---

### ステップ 4: プレイヤーキャラクター

すでにシーンに **Bandit** オブジェクトがある場合:
- 特に追加の設定は不要です

新規に作成する場合:
1. Bandit プレハブをシーンに配置
2. **Bandit.cs** が最新版であることを確認

---

### ステップ 5: 敵オブジェクト（オプション）

1. **Hierarchy** で右クリック → **Create Empty**
2. 名前を `Enemy` に変更
3. **Add Component** → **EnemyHealth** を追加
4. インスペクターで設定:
   - **Max Health**: 100

---

### ステップ 6: LangManager の確認

シーンに **LangManager** が存在することを確認してください。
- ない場合は、新規作成して **LangManager** コンポーネントをアタッチ

---

## ?? テストプレイ

### 1. Play ボタンを押す

### 2. 画面の確認
- 画面下部に技リストが表示される
- 入力フィールドが選択状態になっている（カーソルが点滅）
- **すぐにタイピング可能な状態！**

### 3. 技を使ってみる

**日本語モード:**
```
斬る [Enter]
```

**英語モード:**
```
slash [Enter]
```

### 4. 結果の確認
? 正しい技名の場合:
- Bandit が **攻撃アニメーション** を再生
- 「斬る! 24 ダメージ!」と表示
- 敵の HP が減る（コンソールログ確認）
- 入力フィールドが自動的にクリアされ、再び選択状態に

? 間違った技名の場合:
- 「入力ミス!」と赤文字で表示

### 5. 回避技を試す

```
避ける [Enter]
```
または
```
dodge [Enter]
```

結果:
- Bandit が **ジャンプ** して回避

---

## ?? トラブルシューティング

### 入力フィールドにカーソルが表示されない
**原因**: EventSystem が存在しない
**解決**: Hierarchy → 右クリック → UI → Event System

### 技リストが表示されない
**原因**: TypingUI の設定が不完全
**解決**: TypingUI コンポーネントの Skill List Display が正しく設定されているか確認

### Enter を押しても反応しない
**原因**: InputField の設定が不正
**解決**: 
1. InputField の **Line Type** が "Single Line" であることを確認
2. TypingUI に **InputField** が正しくアサインされているか確認

### 入力フィールドがクリックしないと選択されない
**原因**: TypingUI が InputField を常時選択していない
**解決**: 
1. TypingUI.cs が最新版であることを確認
2. Update() メソッドで SelectInputField() が呼ばれているか確認

### 技を使っても敵にダメージが入らない
**原因**: EnemyHealth が TypingCombatSystem のイベントを購読していない
**解決**: 
1. EnemyHealth コンポーネントが敵オブジェクトにアタッチされているか確認
2. Play モード中にコンソールログを確認

### 攻撃アニメーションが再生されない
**原因**: Animator に "Attack" トリガーが存在しない
**解決**:
1. Bandit の Animator Controller を確認
2. "Attack" という名前のトリガーパラメータを追加

---

## ?? デフォルトで使える技

### 日本語モード
| 技名 | ダメージ | 種類 | 動作 |
|------|---------|------|------|
| 蹴る | 20 | 攻撃 | 攻撃モーション |
| 斬る | 24 | 攻撃 | 攻撃モーション |
| 守る | 10 | 防御 | 防御態勢 |
| 避ける | 24 | 回避 | ジャンプで回避 |
| 強力な斬撃 | 90 | 攻撃 | 攻撃モーション |
| 回転斬り | 52 | 攻撃 | 攻撃モーション |

### 英語モード
| 技名 | ダメージ | 種類 | 動作 |
|------|---------|------|------|
| kick | 40 | 攻撃 | 攻撃モーション |
| slash | 60 | 攻撃 | 攻撃モーション |
| defend | 30 | 防御 | 防御態勢 |
| dodge | 40 | 回避 | ジャンプで回避 |
| powerful slash | 210 | 攻撃 | 攻撃モーション |
| spinning slash | 195 | 攻撃 | 攻撃モーション |

---

## ?? カスタマイズのヒント

### パネルの色を変更
TypingPanel を選択 → Image コンポーネント → Color を変更

### フォントを変更
各 TextMeshPro オブジェクト → Font Asset を変更

### 効果音を追加
1. AudioSource を 2 つ作成
2. それぞれに効果音クリップを設定
3. TypingUI の Correct Sound / Incorrect Sound にドラッグ

### Placeholder の文字を変更
InputField の子オブジェクト "Placeholder" を選択 → Text を変更

---

## ? これで完成！

**InputField を使用することで、以下の利点があります:**
- ? ゲーム開始時から常にテキスト入力可能
- ? 技使用後も自動的に入力フィールドが選択される
- ? クリック不要でタイピングに集中できる
- ? ネイティブな入力体験（IME 対応）

技名を入力して Enter を押すだけで、タイピング戦闘が楽しめます！

質問があれば IMPLEMENTATION_SUMMARY.md を参照してください。
