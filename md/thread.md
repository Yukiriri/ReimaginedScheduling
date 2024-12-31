<!-- # 核心配重策划
| 大核数 | 小核数 | 大核可独占范围 | 共享调度范围 |
| :----- | :----- | :------------- | :----------- |
| N      | 0      | 1--N/2         | N/2--N       |
| N      | >0     | 1--N           | >N           |
-->

# 线程排布
  - ## UE4游戏
    | 核心          | 线程                       |
    | :------------ | :------------------------- |
    | 1 & 超线程    | GameThread                 |
    | 2 & 超线程    | RenderThread & RTHeartBeat |
    | 3 & 超线程    | RHIThread    & AudioThread |
    | 4--N          | 其他                       |

  - ## UE5游戏
    | 核心       | 线程                               |
    | :--------- | :--------------------------------- |
    | 1 & 超线程 | GameThread                         |
    | 2 & 超线程 | RenderThread & RHISubmissionThread |
    | 3 & 超线程 | RHIThread                          |
    | 4--N       | 其他                               |

  - ## Unity游戏
    | 核心       | 线程                      |
    | :--------- | :------------------------ |
    | 1 & 超线程 | GameThread                |
    | 2 & 超线程 | UnityMultiRenderingThread |
    | 3 & 超线程 | UnityGfxDeviceWorker      |
    | 4--N       | 其他                      |

  - ## 其他游戏
    | 核心       | 线程         |
    | :--------- | :----------- |
    | 1 & 超线程 | GameThread   |
    | 2 & 超线程 | RenderThread |
    | 3--N       | 其他         |
