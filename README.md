<div align="center">

[![Banner](https://socialify.git.ci/Yukiriri/ReimaginedScheduling/image?description=1&language=1&name=1&owner=1&pattern=Circuit%20Board&theme=Auto)]()

[![Build](https://img.shields.io/github/actions/workflow/status/Yukiriri/ReimaginedScheduling/build.yml?style=for-the-badge)](
    https://github.com/Yukiriri/ReimaginedScheduling/actions/workflows/build.yml
)
[![Downloads](https://img.shields.io/github/downloads/Yukiriri/ReimaginedScheduling/total?style=for-the-badge)][Release]

[Release]: https://github.com/Yukiriri/ReimaginedScheduling/releases

通过观测前台游戏的线程负载，重新规划线程分配，让高负载主线程独占核心，从而使用完整的单核性能，帮助高端硬件更上一层楼。  
Intel和AMD都可以用，尤其对AMD改善更大，让AMD用户可以同等安心玩游戏。  
由于我拥有的硬件和游戏有限，目前已涵盖范围还比较少，期待能和大家一起完善，如果在特定情况遇到问题或者有建议，欢迎提出。  

</div>

# 食用效果
- ### 食用前
![](./md/img/before.png)
- ### 食用后
![](./md/img/after.png)

# 食用方式
1. 前往 [Release] 下载已编译好的版本
2. 进入目录运行ReimaginedScheduling.Services.exe
3. 开始玩游戏
4. （可选）观察ReimaginedScheduling.Services.exe的控制台输出

# 计划功能
- 控制面板用户UI
- 使用RTSS同等原理给游戏超采样或插帧（我知道有小黄鸭，但是效果差强人意）

# 调优避坑
- ## 系统设置
    <table>
    <tr><th>设置</th>                       <th>系统版本</th>     <th>N卡</th>        <th>A卡</th>               <th>I卡</th></tr>
    <tr><td rowspan="2">硬件加速GPU计划</td> <td>23H2以及以前</td> <td>建议不开</td>   <td rowspan="2">不支持</td> <td rowspan="4">没用过不知道</td></tr>
    <tr>                                    <td>24H2以及以后</td> <td>不开白不开</td> </tr>
    <tr><td rowspan="1">窗口化游戏优化</td>  <td>21H2-24H2</td>    <td colspan="2">不使用老软件可以开</td></tr>
    </table>
- ## AMD注意项
    - ## BIOS注意项
        - ### PSS Support(Cool n Quite)
            ### 强烈建议别关
            关掉后将连带关闭CPPC等诸多自动性能调节  
            带来的严重影响：就算帧率再高，Frametime再直线，游戏渲染出来也是一顿一顿  
            打开`Windows事件查看器`，筛选`Kernel-Processor-Power`事件，会看见`ACPI None`，而正常情况是`ACPI CPPC`  
        - ### CPPC PC
            ### 建议保持Auto
            关掉后会将所有核心的性能上限同步为最雷的核心的上限  
            打开`Windows事件查看器`，筛选`Kernel-Processor-Power`事件，也可以看见变化  
        - ### Global C State
            ### 建议保持Auto
            如果启用这个选项会影响软件计算CPU的正确占用率，那就关闭  
    - ## 电源计划注意项
        不要再尝试 `最小放置核心50%` + `SMT循环` 的组合  
        这个组合搭配我的程序会让Windows水土不服  

## 致谢
- [dahall/Vanara](https://github.com/dahall/Vanara)

## Stargazers
[![Stargazers](https://starchart.cc/Yukiriri/ReimaginedScheduling.svg?variant=adaptive)](https://starchart.cc/Yukiriri/ReimaginedScheduling)
