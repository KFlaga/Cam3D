#pragma once

#include "TaskQueue.hpp"
#include <functional>

namespace cam3d
{
	struct PerPixelFunction
	{
		using FunctionType = std::function<void(int pixRow, int pixCol)>;

		struct Rect
		{
			int startRow;
			int startCol;
			int endRow;
			int endCol;
		};

		static void run(FunctionType mainFun, int rows, int cols);

		static void runForRect(FunctionType mainFun, Rect rect);

		// Exectues funcs in parallel tasks - each task computes 'rowsInTask' rows
		static std::vector<Task> getParallelTasks(FunctionType mainFun, int rows, int cols, int rowsInTask, TaskId startId);

		static std::vector<Task> getParallelTasksForRect(FunctionType mainFun, Rect rect, int rowsInTask, TaskId startId);

		// Executes 'mainFun' on pixels in range (y:[bh,rows-bh], x:[bw,cols-bw]) row-wise and
		// 'borderFun' for rest of pixels (on border)
		static void runWithBorder(FunctionType mainFun, FunctionType borderFun,
			int borderWidth, int borderHeight, int rows, int cols);

		// Exectues funcs in parallel tasks - each task computes 'rowsInTask' rows
		static std::vector<Task> getParallelTasksWithBorder(FunctionType mainFun, FunctionType borderFun,
			int borderWidth, int borderHeight, int rows, int cols, int rowsInTask, TaskId startId);
	};
}
