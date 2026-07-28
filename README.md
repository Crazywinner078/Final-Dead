# Final Dead

一个使用 Unity 制作的 3D 第一人称密室推理逃脱原型项目。项目以“在封闭房间中通过调查线索、收集道具、组合物品、解开机关并触发最终演出”为核心体验，目标是完成一个可完整游玩的求职向 Gameplay Vertical Slice。

本项目不是 FPS 射击项目，左轮手枪只作为最终机关和结局演出的一部分使用。

## 项目信息

- 开发引擎：Unity `2022.3.62f3c1`
- 开发语言：C#
- 项目类型：3D 第一人称 / 密室逃脱 / 推理解谜
- 渲染管线：Built-in Render Pipeline
- 输入方式：键盘 + 鼠标
- 版本管理：Git + Git LFS

## 游戏内容

玩家需要在一个封闭房间内探索环境，通过中心射线调查物体、拾取关键道具，并利用背包中的道具推进谜题流程。

当前实现的主要玩法包括：

- 第一人称移动与鼠标视角控制
- 中心准星射线检测与交互提示
- 可调查物品、可拾取物品、条件交互物体
- 背包系统：物品选择、调查、取出、收起、组合
- 钥匙开启抽屉、道具藏匿与拾取
- 保险柜四位密码输入与开启动画
- 笔记本电脑密码输入与线索图片展示
- 四灯机关特写观察与循环闪烁序列
- 最终台座升起、左轮手枪装弹、开枪演出与结局 UI
- 主菜单、设置界面、音量调节、退出游戏
- BGM、调查、拾取、输入、装弹、开枪等音效反馈

## 操作方式

| 按键 | 功能 |
| --- | --- |
| `WASD` | 移动 |
| 鼠标 | 转动视角 |
| `E` | 互动 / 确认 |
| `Tab` | 打开或关闭背包 |
| `Esc` | 关闭当前 UI / 退出当前查看状态 |
| `F1` | 游戏内设置界面 |

## 技术实现

### 第一人称玩家控制

玩家使用 Unity 自带的 `CharacterController` 作为移动基础，移动逻辑和镜头逻辑拆分到不同脚本中：

- `PlayerMotor`：负责移动、重力和角色控制器调用
- `PlayerLook`：负责水平转身、垂直抬头低头和角度限制
- `PlayerInteractor`：负责从摄像机中心发射射线，检测可交互物体
- `PlayerModeController`：负责切换自由移动、背包、调查、设置、演出等状态

这样做的好处是每个脚本职责清晰，后续要扩展蹲下、冲刺、镜头特写、UI 锁定时，不需要把所有逻辑堆在一个 Player 脚本里。

### 接口驱动的交互系统

项目使用 `IInteractable` 统一所有场景交互对象。`PlayerInteractor` 只负责检测目标并调用 `Interact()`，并不关心目标到底是道具、抽屉、保险柜、电脑还是机关。

目前已有的交互类型包括：

- `ExamineInteractable`：调查文本或线索
- `PickUpInteractable`：拾取道具并进入背包
- `DrawerInteractable`：抽屉开启与锁定状态
- `RequiredHeldItemInteractable`：需要手持指定物品才能触发
- `SafePuzzleController`：保险柜密码与动画
- `LaptopPuzzleController`：电脑密码与线索展示
- `RevolverInteractable`：最终左轮装弹与开枪逻辑

这个结构的优势是扩展成本低。新增一个机关时，只需要实现或继承交互接口，不需要改玩家射线检测代码。

### ScriptableObject 数据驱动背包

道具数据使用 `ItemDataSO` 管理，组合配方使用 `ItemCombinationRecipeSO` 管理。道具的名称、描述、调查文本、图标、是否可取出、是否可组合、手持模型等内容都可以在 Inspector 中配置。

这样可以减少硬编码，让内容调整更接近真实项目中的配置流程。例如：

- 钳子 + 衣架 = 简易钩子
- 电池 + 相机 = 装入电池的相机
- 某些道具可以取出并用于场景交互
- 某些道具只能调查或用于组合

### 背包与 UI 状态管理

背包 UI 支持物品 slot、选中高亮、行动菜单、调查 tooltip、线索图片展示和拾取确认。

实现上把 UI 展示和背包数据分开：

- `PlayerInventory` 保存物品列表、选中物品和手持物品
- `InventoryUI` 负责背包界面、按钮逻辑和组合操作
- `InventorySlotUI` 负责单个物品格子的显示与点击回调
- `InventoryTooltipUI` 负责调查文本提示
- `PickupConfirmUI` 负责关键道具获得时的确认感
- `ClueImageUI` 负责纸条、照片等图片线索展示

背包关闭时会重置临时菜单和选中状态，避免 UI 状态残留影响下一次操作。

### 谜题控制器拆分

每个复杂谜题都有独立 Controller，不把所有规则集中写在一个全局 GameManager 里：

- `SafePuzzleController`：四位密码输入、数字滚动、正确密码校验、保险柜开启动画
- `LaptopPuzzleController`：手持 USB 后进入电脑密码界面，输入正确后显示线索图
- `FourLightPuzzleController`：四灯机关的固定序列闪烁、随机重置闪烁和特写观察
- `FinalRevealSequence`：最终机关解锁后，房间中央台座升起并展示左轮与子弹
- `RevolverEndingSequence`：开枪阶段镜头锁定、手枪移动到镜头前、BGM 淡出、结局 UI 衔接

这种拆分让每个谜题都能独立调试，也便于在简历或面试中单独讲清楚某个系统。

### 镜头特写与演出流程

项目中存在多个“玩家不能自由移动，需要锁定视角观看”的场景，例如四灯机关特写和最终左轮演出。

相关逻辑通过 `PlayerModeController` 和 `CameraFocusController` 配合完成：

- 进入特写时关闭玩家移动、视角旋转和普通交互
- 摄像机平滑移动到指定观察点
- 退出特写后恢复玩家控制
- 最终演出阶段锁定流程，避免玩家重复触发或打断动画

这套模式比直接在每个脚本里开关鼠标和移动更稳定，后续也能复用到更多调查物或剧情演出上。

### 音频系统

音效和音乐没有直接散落在各个脚本里播放，而是拆成了可复用的小系统：

- `AudioCueSO`：用 ScriptableObject 保存音效配置
- `AudioCuePlayer`：统一播放交互、拾取、输入、错误、装弹、开枪等音效
- `MusicPlayer`：负责 BGM 播放、停止、淡入淡出和音量控制
- `AudioSettingsUI`：通过 Slider 调节 BGM 和 SFX 音量，并使用 `PlayerPrefs` 保存设置

这样做方便统一调音量，也能避免每个交互物体都自己持有 AudioSource。

### Git 与项目工程管理

Unity 项目使用 `.gitignore` 排除了 `Library/`、`Temp/`、`Logs/`、`obj/`、`.vs/` 等自动生成目录，避免仓库膨胀或提交无意义文件。

模型、贴图、字体、音频等二进制素材使用 Git LFS 管理，降低普通 Git 仓库压力，也更适合 Unity 项目协作。

## 技术优势

这个项目比较适合展示以下客户端 / Gameplay 开发能力：

- 能从 0 到 1 搭建一个完整可玩的 3D 原型，而不是只完成单个教程功能
- 能将玩家控制、交互、UI、背包、谜题、音频、镜头演出拆成相对独立的模块
- 使用接口和基类降低交互系统耦合，便于扩展新谜题和新物品
- 使用 ScriptableObject 管理道具和配方，减少硬编码，提高内容配置效率
- 能处理游戏中常见的状态切换问题，例如背包、调查、设置、特写、结局演出之间的控制权切换
- 具备基础的 UI 反馈意识，包括交互提示、拾取确认、调查弹窗、线索图片和音量设置
- 能完成从编辑器内测试到 Git/GitHub/LFS 管理的项目收尾流程

## 项目结构

```text
Assets/_Project/
  Music/              音乐、音效和音频配置
  Prefabs/            项目自定义预制体
  Scenes/             主菜单和游戏场景
  Scripts/
    Audio/            音频播放与音量设置
    Camera/           镜头特写控制
    Data/             道具和组合配方 ScriptableObject
    Editor/           编辑器辅助工具
    Interaction/      交互接口与交互物体
    Player/           玩家移动、视角、背包、手持物
    Puzzle/           保险柜、电脑、四灯机关、最终演出
    UI/               背包、提示、调查、密码、结局、设置 UI
  Sprite/             UI 图片和线索图片
```

第三方或导入模型主要位于：

```text
Assets/Model/
Assets/Prefab/
```

## 如何运行

1. 安装 Git LFS。
2. 克隆仓库。
3. 在仓库目录执行：

```powershell
git lfs pull
```

4. 使用 Unity Hub 打开项目。
5. Unity 版本选择 `2022.3.62f3c1`。
6. 打开场景：

```text
Assets/_Project/Scenes/MainMenu.unity
```

也可以直接打开游戏主场景：

```text
Assets/_Project/Scenes/SampleScene.unity
```

## 说明

本项目为个人学习与求职展示用途。项目中的部分素材、字体、音效或视觉风格参考来自第三方资源，正式公开发布或商业使用前需要重新核查授权，或替换为原创 / 可商用素材。
