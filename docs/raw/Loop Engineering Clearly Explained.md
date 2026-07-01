---
title: "Loop Engineering Clearly Explained"
source: "https://x.com/akshay_pachaar/article/2069118430582866051"
author:
  - "[[Akshay 🚀 (@akshay_pachaar)]]"
published: 2026-06-22
created: 2026-06-28
description: "Half your feed is suddenly saying the same thing. Stop prompting your agents, start engineering loops.Boris Cherny, the person who built Cla..."
tags:
  - "clippings"
---
Loop Engineering Clearly Explained

To view keyboard shortcuts, press question mark
View keyboard shortcuts


Loop Engineering Clearly Explained 
Akshay 🚀
@akshay_pachaar
·
Jun 22

Half your feed is suddenly saying the same thing. Stop prompting your agents, start engineering loops.
Boris Cherny, the person who built Claude Code, said it plainly: "I don't prompt Claude anymore. I have loops that are running. My job is to write loops."
The person who builds one of the most popular coding agents on earth doesn't prompt it. So what is he doing instead?
That's the whole idea behind loop engineering. Now let's break down why it's harder than it looks.
First, the loop itself
An agent isn't a magic box. At its core, it's a plain loop:
python
while True:
    response = model(context)
    if response.has_tool_calls():
        results = run_tools(response.tool_calls)
        context += results
    else:
        break
The model reads the context. It asks to call a tool. You run the tool and feed the result back. The model reads again, and this repeats until it stops asking for tools.
Model → tools → context → repeat.
Here's the part that surprises people. This loop is already solved. Every serious agent framework lands on roughly these six lines. Nobody is competing on the while statement.
So if the loop is trivial, what is everyone actually engineering?
The work moved outside the model
The center of gravity in AI keeps drifting away from the model itself.
Prompt engineering. The words you send.
Context engineering. Everything the model sees, not just your instructions.
Harness engineering. The code around the model that runs tools, tracks state, and handles errors.
Loop engineering. The autonomous cycle that drives the whole thing toward a goal.
Each layer wraps the one before it. You didn't stop caring about prompts. You just realized the prompt is one small piece of a much bigger system.
LangChain puts it cleanly. Agent = Model + Harness. If you're not the model, you're the harness.
And here's the finding that should reorder your priorities. The harness now matters more than the model. Teams have kept the model fixed, changed only the code around it, and jumped from the middle of a benchmark into the top five. Same brain, different loop.
Loop engineering is the discipline of building everything that brain runs inside. Let me show you the parts that actually break.
Hard part 1: knowing when to stop
This is the problem nobody warns you about.
When an agent stops asking for tools, it has ended its turn. That is not the same as finishing the job.
Picture a coding agent. It writes some code, glances around, sees that progress was made, and announces it's done. The tests still fail. It declared victory anyway.
A terminal message ends the turn, not the task. Confusing those two is the most common way loops go wrong.
Good loops stop for the right reasons, so you layer several brakes:
Max iterations. A hard cap so a stuck agent can't run forever.
Budget and time limits. A ceiling on tokens, money, and seconds.
No-progress detection. If it repeats the same call with the same arguments, it's spinning.
A real completion check. An automated condition proving the job is done.
That last one carries the weight. "Done" should mean the tests pass, not the agent feeling good about its work.
Hard part 2: keeping the context clean
Long loops rot from the inside.
The more turns an agent takes, the more junk piles into its context, like old tool outputs, dead ends, and stale reasoning. Model performance drops as that pile grows. The field calls it context rot.
A loop makes it spiral. A rotted context produces a worse decision, which adds more noise, which rots the context further. People call this the doom loop, and you've felt it. The agent gets dumber the longer it runs.
You fight it by treating context as a budget, not a bucket:
Compaction. Summarize the conversation when it gets long, then continue from the summary.
Offloading. Push huge outputs to a file and keep only the slice you need.
Sub-agents. Hand a messy subtask to a separate agent and let only its clean result return.
The instinct is to keep everything, just in case. The skill is knowing what to throw away.
Hard part 3: tools the agent can actually use
A loop is only as good as the tools inside it.
Pile on a hundred tools and the agent loses track of which one to reach for. A tight set of focused, non-overlapping tools wins. Anthropic's rule of thumb is sharp. If a human engineer can't say for certain which tool fits, the agent has no chance.
Two things matter more than people expect:
Make writes safe to repeat. Loops retry, and if a retried "create customer" call makes a second customer, you'll wake up to duplicate records and double billing. Anything that changes state has to be safe to call twice.
Write error messages for the agent, not the human. A good error tells the agent what to do next. Before a tool ships, ask whether an LLM reading its error would know the next move.
In a loop, an error isn't a dead end. It's the next instruction.
Hard part 4: something that can say no
Autonomous loops have a quiet failure mode. An agent left alone tends to agree with itself.
The sharpest comment in the whole debate nailed it. Designing the loop is half the job, and the other half is putting something in the loop that can say no, like a test, a type check, or a real error.
A loop with no critic is just an agent nodding along to its own work.
The fix is to separate the maker from the checker. One model does the work. A different check, often a separate model or a hard test, grades it. The worker doesn't grade its own homework.
The actual shift
Now Cherny's quote makes sense.
Prompting is you steering the agent move by move. Loop engineering is you building the system that steers it, then stepping back.
Your job changes from giving instructions to designing three things:
The goal, written as success criteria the agent can check itself against.
The loop, with sane brakes so it stops well.
The verifier, so "done" is proven, not claimed.
Andrej Karpathy captures the mindset. Don't tell the model what to do, give it success criteria and watch it go. He runs research loops overnight that tweak a script, test it, keep what works, and discard what doesn't, with himself nowhere in the loop. He arranges it once and hits go.
That's the whole move. You stop being the hands and become the person who designs the machine.
Where to start
You don't need an overnight autonomous agent on day one. Build up to it:
Start with the basic loop, and add a max-iteration cap, a timeout, and a cost ceiling right away.
Define "done" as an automated check before you begin, not a vibe afterward.
Protect the context. Compact long runs, offload big outputs, isolate messy subtasks.
Audit your tools. Keep them few and focused, make writes safe to repeat, and rewrite errors so an agent can act on them.
Put a critic in the loop. Only go fully hands-off once you trust the thing that says no.
The takeaway
Loop engineering isn't a framework or a tool you install. It's a shift in where you aim your effort.
The model is becoming a commodity. The loop around it is where the real engineering lives now.
The best builders stopped asking "what should I tell the agent to do?" They started asking "what system would do this without me?"
Answer that one well, and you'll stop prompting too.
Here's a summary of
Key takeaways in loop engineering.
Thanks for reading!
Cheers :)
Akshay.
Want to publish your own Article?
Upgrade to Premium
Akshay 🚀
@akshay_pachaar
Simplifying LLMs, AI Agents, RAG, and Machine Learning for you! • Co-founder 
@dailydoseofds_
• BITS Pilani • 3 Patents • ex-AI Engineer @ LightningAI
