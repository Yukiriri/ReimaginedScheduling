
# 线程调整项
  - ## UE4游戏
    | 核心  | 线程         |
    | :---- | :----------- |
    | +0    | GameThread   |
    | +1    | RenderThread |
    | +2    | RHIThread    |
    | +3--N | 其他         |

  - ## UE5游戏
    | 核心  | 线程                |
    | :---- | :------------------ |
    | +0    | GameThread          |
    | +1    | RenderThread        |
    | +2    | RHIThread           |
    | +3    | RHISubmissionThread |
    | +4--N | 其他                |

  - ## Unity游戏
    | 核心  | 线程                      |
    | :---- | :------------------------ |
    | +0    | Main                      |
    | +1    | UnityMultiRenderingThread |
    | +3    | UnityGfxDeviceWorker      |
    | +4--N | 其他                      |

  - ## 其他游戏
    | 核心  | 线程         |
    | :---- | :----------- |
    | +1    | Main         |
    | +2    | RenderThread |
    | +3--N | 其他         |
