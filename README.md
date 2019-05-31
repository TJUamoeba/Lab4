# 实验4：C# .NET 综合应用程序开发 #
## 一、功能概述 ##
![](https://github.com/TJUamoeba/Lab4/blob/master/Pictures/%E4%B8%BB%E7%AA%97%E5%8F%A3.png)
1. 串口状态选择模块，进行串口、波特率选择，串口连接和断开
2. log文件状态模块，将数据以Json格式保存
3. 接收数据模块，显示实时数据和对应的转换值、发送指令
4. 图像模块，包括两个zedGraph图像显示温度、光强随时间变化的变化曲线，文本框显示当前光强/温度转换值
5. 灯光调控模块，5个Slider表示设置对应的黄绿蓝红白灯的灯光强度，下方按钮显示RGB混合色，发送按钮发送控制Arduino LED明暗的指令；

## 二、 项目特色 ##
1. 实现了WPF控件的自适应，使用了合理的页面规划（Grid、StackPanel）
2. 使用两个zedgraph控件分别显示温度、光强
3. 实现了将数据保存到Json文件的功能

## 三、 代码总量 ##
700+

## 四、 工作时间 ##
1周

## 五、 结论  ##

通过本实验，我对WPF编程有了更深一步的理解，主要归纳为以下几点：
1.  委托事件在UI更新中有重要作用，可以避免非主线程调用控件更新导致无响应的情况；
2.  binding绑定数据与控件，可以避免更新UI时产生混乱；
3.  学习了多种新的控件的使用方法，如：
	comboBox-下拉更新
	Slider-使用委托监视value的改变
	ZedGraph-图像的创建、动态更新
4.  对于串口调用有了更深的理解
实验中存在很多可以改进的地方，在之后的学习中我也会继续寻找改进的方法。

