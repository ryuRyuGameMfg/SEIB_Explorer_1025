# ローカライズ一括管理システム

## 概要

このシステムを使用すると、各UIコンポーネントに個別にLocalizedStringを設定する必要がなくなり、ScriptableObjectで一括管理できます。

## 使用方法

### 1. LocalizationConfigアセットの作成

1. Unityエディタで `Assets > Create > SEIB Explorer > Localization Config` を選択
2. 作成されたアセットを選択
3. Inspectorで `String Table` に使用するStringTableアセットを設定

### 2. マッピングの追加

Inspectorの `Mappings` リストで直接マッピングを追加・編集します。

**各マッピングの設定項目:**
- `Game Object Path`: Hierarchy内のGameObjectのパス（例: `Canvas/Panel/TitleText`）
- `Component Type`: コンポーネントタイプ（`Text`, `TextMeshProUGUI`, `SliderTimeRange`, `UImanager`）
- `Localization Key`: StringTable内のキー名
- `Arguments`: 動的な値のリスト（必要に応じて追加）
- `Enabled`: このマッピングを有効にするか

**追加方法:**
- `Mappings` リストの「+」ボタンをクリック
- 各項目を設定

**削除方法:**
- 各マッピングの「-」ボタンをクリック

### 3. シーンに自動設定

1. 設定したいシーンを開く
2. LocalizationConfigアセットを選択
3. Inspector下部の「シーン内のUI要素に自動設定」ボタンをクリック
4. すべてのUI要素に自動的にLocalizedStringが設定されます

## コンポーネントタイプ

### Text / TextMeshProUGUI
- `LocalizedStringComponent`が自動的に追加されます
- 文字列が自動的に更新されます

### SliderTimeRange
- `localString`フィールドに自動設定されます
- 既存の動作を維持します

### UImanager
- フィールド名をキー名として使用します（例: `latString`）
- 各LocalizedStringフィールドに自動設定されます

## メリット

1. **一括管理**: すべてのローカライズ設定を1箇所で管理
2. **ScriptableObject**: Unityエディタ内で直接編集可能
3. **自動設定**: ボタン1つで全UI要素に設定
4. **保守性**: 新しいUI要素を追加する際もマッピングを1つ追加するだけ

## 注意事項

- GameObjectのパスは正確に記述してください
- StringTableにキーが存在することを確認してください
- 設定後は実際に動作確認してください

