<div align="center">

[![Banner](https://socialify.git.ci/Yukiriri/ReimaginedScheduling/image?description=1&language=1&name=1&owner=1&pattern=Circuit%20Board&theme=Auto)]()

[![Build](https://img.shields.io/github/actions/workflow/status/Yukiriri/ReimaginedScheduling/build.yml?style=for-the-badge)](
  https://github.com/Yukiriri/ReimaginedScheduling/actions/workflows/build.yml
)
[![Downloads](https://img.shields.io/github/downloads/Yukiriri/ReimaginedScheduling/total?style=for-the-badge)][Release]

[Release]: https://github.com/Yukiriri/ReimaginedScheduling/releases

通过观测前台游戏的线程负载，重新规划线程分配，让高负载主线程独占核心，从而使用完整的单核性能，帮助高端CPU更上一层楼。  
Intel和AMD都可以用，尤其对AMD改善更大，让AMD用户可以同等安心玩游戏。  
由于我拥有的硬件和游戏有限，目前已涵盖范围还比较少，期待能和大家一起完善，如果在特定情况遇到问题或者有建议，欢迎提出。  

</div>

> [!IMPORTANT]  
> 如果出现这条提示，就说明项目正在完全重新计划，近期的新构建将是空壳蓝图版本，不带任何有效功能  
> 先重点看readme的实现合理性  

# 实现原理
- ## 运行流程
  ```mermaid
  graph LR;
    开始-->A[判断前台进程]-->B{符合游戏占用};
    B--否-->A;
    B--是-->统计线程信息-->安插线程新分布-->A;
  ```
- ## 线程分布方式
  - ## UE引擎游戏
    <table>
    <tr><th></th>           <th>GameThread</th><th>RenderThread</th><th>RHIThread</th><th>Foreground Worker</th><th>其他</th></tr>
    <tr><th>8x2大核</th>     <td>核1</td>       <td>核2</td>         <td>核3</td>      <td>核4-8</td>            <td>核9-16</td></tr>
    <tr><th>6x2大核</th>     <td>核1</td>       <td>核2</td>         <td>核3</td>      <td>核4-6</td>            <td>核7-12</td></tr>
    <tr><th>10大核</th>      <td>核1</td>       <td>核2</td>         <td>核3</td>      <td>核4-6</td>            <td>核7-10</td></tr>
    <tr><th>8大核大小核</th> <td>P核1</td>      <td>P核2</td>        <td>P核3</td>     <td>P核4-8</td>            <td>全E核</td></tr>
    <tr><th>8大核</th>       <td>核1</td>       <td>核2</td>         <td>核3</td>      <td>核4-6</td>            <td>核7-8 + 超线程</td></tr>
    <tr><th>6大核大小核</th> <td>P核1</td>      <td>P核2</td>        <td>P核3</td>     <td>P核4-6</td>            <td>全E核</td></tr>
    <tr><th>6大核</th>       <td>核1</td>       <td>核2</td>         <td colspan="3">核3-6 + 超线程</td></tr>
    <tr><th>4大核大小核</th> <td>P核1</td>      <td>P核2</td>        <td colspan="3">P核3-4 + 超线程 + 全E核</td></tr>
    <tr><th>4大核</th>       <td>核1</td>       <td colspan="4">核2-4 + 超线程</td></tr>
    </table>
  - ## 其他游戏
    |MainThread|RenderThread|其他|
    |:-|:-|:-|
    |核心1|核心2|核心3-N|

# 食用效果
- ### 食用前
![](./md/img/before.png)
- ### 食用后
![](./md/img/after.png)

# 食用方式
1. 前往 [Release] 下载V52版本
2. ### 选择运行方式
  - ### 方式1  
    直接运行ReimaginedScheduling.Services.exe  
    开始玩游戏  
  - ### 方式2  
    传入参数运行（适合搭配快捷方式）  
    ```
    start "...\ReimaginedScheduling.Services.exe" "...\游戏.exe"
    ```

> [!TIP]
> 总物理核心低于4核就不建议使用了，正如我所说，我的程序是让高端CPU更上一层楼  
> 除非尝试运行后确认可以缓解某些瓶颈，不然大概率在低核心CPU上会是负优化  

# 计划功能
- 控制面板用户UI

# 调优避坑
- ## 系统设置
  <table>
  <tr><th>设置</th>                        <th>系统版本</th>     <th>N卡</th>       <th>A卡</th>                <th>I卡</th></tr>
  <tr><th rowspan="2">硬件加速GPU计划</th> <td>23H2以及以前</td> <td>建议不开</td>   <td rowspan="2">不支持</td> <td rowspan="4">没用过不知道</td></tr>
  <tr>                                    <td>24H2以及以后</td> <td>不开白不开</td> </tr>
  <tr><th rowspan="1">窗口化游戏优化</th>  <td>21H2-24H2</td>    <td colspan="2">不使用老软件可以开</td></tr>
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

## Stargazers
[![Stargazers](https://starchart.cc/Yukiriri/ReimaginedScheduling.svg?variant=adaptive)](https://starchart.cc/Yukiriri/ReimaginedScheduling)
