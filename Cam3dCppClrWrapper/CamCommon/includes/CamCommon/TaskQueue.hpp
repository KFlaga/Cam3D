#pragma once

#include <vector>
#include <initializer_list>
#include <functional>
#include <atomic>
#include <future>
#include <mutex>
#include <map>

namespace cam3d
{
using TaskId = std::size_t;

struct Task
{
    TaskId id;
    std::function<void()> task;

	Task() {}
	Task(TaskId id, std::function<void()> task) : id{id}, task{task} {}
};

class StaticTaskQueue
{
    using TaskIndex = std::size_t;
    struct Item
    {
        Task task;
        TaskIndex index;
        std::vector<TaskId> dependencies;
        int dependencyCounter;
    };

    std::vector<std::future<TaskIndex>> runningTasks;
    std::size_t maxRunningTasks;

    std::vector<Item> allTasks;
    std::vector<TaskIndex> readyTasks;
    TaskIndex nextTaskIdx;

    std::map<TaskId, TaskIndex> idToIndex;

    using DependentOnList = std::vector<TaskIndex>;
    std::vector<DependentOnList> dependencyGraph;

    std::atomic_bool shouldEnd;
	bool running;

public:
    StaticTaskQueue(std::size_t maxRunningTasks);
    ~StaticTaskQueue();

    void addTask(Task task, const std::vector<TaskId>& dependencies);
    void end();
    void run();
    std::size_t getTaskCount() const { return allTasks.size(); }
    std::size_t getTaskDoneCount() const { return nextTaskIdx; }
	bool isAllDone() const;
	bool isRunning() const { return running; }

private:
    bool isAllReadyQueued() const;
    std::pair<TaskIndex, Task> getNextTask();
    void runAvailableTasks();
    void updateFinishedTasks();
    void prepare();
    void updateGraph(int doneIdx);
};
}
