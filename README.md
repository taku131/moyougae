# moyougae

`moyougae` は、Meta Quest 上で実空間に仮想家具を配置するための Unity 製 MR プロトタイプです。

本アプリでは、部屋の境界情報の入力とコントローラーのレイ操作を用いて簡易的な部屋形状を作成し、その上で家具モデルの配置、選択、移動、拡大縮小を mixed reality 環境で行うことができます。

## 主な機能

- Meta Quest / Meta XR を用いた MR シーン構築
- コントローラーのレイ入力による部屋の境界点選択
- 床、壁、ドア、窓を含む簡易的な部屋メッシュ生成
- 家具カタログからの家具配置と選択
- 配置済み家具の移動・拡大縮小操作
- GLB モデルの読み込みと登録
- 生成モデルのランタイム管理
- インポートしたモデル向けの URP マテリアル調整

## 使用技術

- Unity 2022.3.37f1
- C#
- Universal Render Pipeline
- Meta XR SDK
- XR Interaction Toolkit
- glTFast
- OpenUPM

## プロジェクト構成

```text
Assets/
  Scenes/              Unity シーン
  Script/              メインアプリケーションのスクリプト
  material/            家具・部屋関連のアセット
Packages/              Unity パッケージの manifest / lock ファイル
ProjectSettings/       Unity プロジェクト設定

## 主なスクリプト

- `RoomScanManager.cs`  
  部屋の境界点選択とスキャン状態を管理します。

- `RoomBuilder.cs`  
  選択された点をもとに部屋のジオメトリを生成します。

- `FurnitureManager.cs`  
  家具カタログ、家具の配置、生成された GLB モデルの登録、永続化を管理します。

- `FurnitureContextUI.cs`  
  選択中の家具に対する UI 操作を管理します。

- `VREditSpawnManager.cs`  
  コーンを用いた部屋編集と家具配置フローを管理します。

- `GeneratedModelImporter.cs` / `CaptureToModelFlow.cs`  
  生成された GLB モデルをアプリ内で利用するための処理を行います。

## 動作要件

- Unity 2022.3.37f1
- Android Build Support
- Meta Quest デバイス
- Git LFS

## セットアップ

1. リポジトリをクローンします。
2. Git LFS で管理されている大容量アセットを取得します。

```bash
git lfs pull
