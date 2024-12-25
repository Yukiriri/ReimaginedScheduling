# 核心配重策划
| 大核数 | 小核数 | 大核可独占范围 | 大核共享调度范围 |
| :----- | :----- | :------------- | :--------------- |
| N      | 0      | 1-N/2          | N/2-N            |
| N      | >0     | 1-N            | >N               |

# 线程排布
  - ## UE游戏
    | 大核 | 线程                               |
    | :--- | :--------------------------------- |
    | 1    | GameThread                         |
    | 2    | RenderThread + RHISubmissionThread |
    | 3    | RHIThread                          |
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
