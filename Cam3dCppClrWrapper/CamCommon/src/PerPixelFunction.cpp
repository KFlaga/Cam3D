#include "PerPixelFunction.hpp"

namespace cam3d
{
	void PerPixelFunction::runForRect(FunctionType mainFun, Rect rect)
	{
		for (int y = rect.startRow; y < rect.endRow; ++y)
		{
			for (int x = rect.startCol; x < rect.endCol; ++x)
			{
				mainFun(y, x);
			}
		}
	}

	std::vector<Task> PerPixelFunction::getParallelTasksForRect(FunctionType mainFun, Rect rect, int rowsInTask, TaskId startId)
	{
		int taskCount = (rect.endRow - rect.startRow + rowsInTask - 1) / rowsInTask;
		std::vector<Task> tasks;
		tasks.resize(taskCount);

		for (int i = 0; i < taskCount - 1; ++i)
		{
			tasks[i].id = startId++;
			tasks[i].task = [&mainFun, rowsInTask, &rect, i]()
			{
				runForRect(mainFun, { rect.startRow + rowsInTask * i, rect.startRow + rowsInTask * (i + 1), rect.startCol, rect.endCol });
			};
		}
		tasks.back().id = startId;
		tasks.back().task = [&mainFun, rowsInTask, &rect, taskCount]()
		{
			runForRect(mainFun, { rect.startRow + rowsInTask * (taskCount - 1), rect.endRow, rect.startCol, rect.endCol});
		};

		return tasks;
	}

	void PerPixelFunction::run(FunctionType mainFun, int rows, int cols)
	{
		runForRect(mainFun, { 0, rows, 0, cols });
	}

	std::vector<Task> PerPixelFunction::getParallelTasks(FunctionType mainFun, int rows, int cols, int rowsInTask, TaskId startId)
	{
		return getParallelTasksForRect(mainFun, { 0, rows, 0, cols }, rowsInTask, startId);
	}

	void PerPixelFunction::runWithBorder(FunctionType mainFun, FunctionType borderFun,
		int borderWidth, int borderHeight, int rows, int cols)
	{
		int maxX = cols - borderWidth;
		int maxY = rows - borderHeight;
		for (int y = borderHeight; y < maxY; ++y)
		{
			for (int x = borderWidth; x < maxX; ++x)
			{
				mainFun(y, x);
			}
		}

		// 1) Top border
		for (int y = 0; y < borderHeight; ++y)
			for (int x = 0; x < cols; ++x)
				borderFun(y, x);
		// 2) Right border
		for (int y = borderHeight; y < rows - borderHeight; ++y)
			for (int x = cols - borderWidth; x < cols; ++x)
				borderFun(y, x);
		// 3) Bottom border
		for (int y = rows - borderHeight; y < rows; ++y)
			for (int x = 0; x < cols; ++x)
				borderFun(y, x);
		// 4) Left border
		for (int y = borderHeight; y < rows - borderHeight; ++y)
			for (int x = 0; x < borderWidth; ++x)
				borderFun(y, x);
	}

	std::vector<Task> PerPixelFunction::getParallelTasksWithBorder(FunctionType mainFun, FunctionType borderFun,
		int borderWidth, int borderHeight, int rows, int cols, int rowsInTask, TaskId startId)
	{
		std::vector<Task> tasks{ std::forward<std::vector<Task>>(
			getParallelTasksForRect(mainFun, { borderHeight , rows - borderHeight, borderWidth, cols - borderWidth }, rowsInTask, startId))
		};

		tasks.emplace_back(startId + tasks.size(), [&borderFun, borderHeight, borderWidth, rows, cols]() {
			// 1) Top border
			for (int y = 0; y < borderHeight; ++y)
				for (int x = 0; x < cols; ++x)
					borderFun(y, x);
			// 2) Right border
			for (int y = borderHeight; y < rows - borderHeight; ++y)
				for (int x = cols - borderWidth; x < cols; ++x)
					borderFun(y, x);
			// 3) Bottom border
			for (int y = rows - borderHeight; y < rows; ++y)
				for (int x = 0; x < cols; ++x)
					borderFun(y, x);
			// 4) Left border
			for (int y = borderHeight; y < rows - borderHeight; ++y)
				for (int x = 0; x < borderWidth; ++x)
					borderFun(y, x);
		});

		return tasks;
	}
}