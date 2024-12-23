# 核心配重策划
| 大核数 | 小核数 | 大核可独占范围 | 最低共享范围 |
| :----- | :----- | :------------- | :----------- |
| >0     | 0      | 1-N/2          | N/2-N        |
| >0     | >0     | 1-N            | >N           |

# 线程排布
  - ## UE游戏
    | 大核 | 线程                               |
    | :--- | :--------------------------------- |
    | 1    | GameThread                         |
    | 2    | RenderThread + RHISubmissionThread |
    | 3    | RHIThread                          |
    | 4-N  | Foreground Worker #N               |
    | 4-N  | 其他                               |

  - ## Unity游戏
    | 大核 | 线程                      |
    | :--- | :------------------------ |
    | 1    | GameThread                |
    | 2    | UnityMultiRenderingThread |
    | 3    | UnityGfxDeviceWorker      |
    | 4-N  | 其他                      |

  - ## 其他游戏
    | 大核 | 线程         |
    | :--- | :----------- |
    | 1    | GameThread   |
    | 2    | RenderThread |
    | 3-N  | 其他         |
