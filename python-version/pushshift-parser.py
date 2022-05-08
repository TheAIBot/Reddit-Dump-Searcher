import time


dumpPath = r"Q:\Github\PushShift-Dump-Parser\PushShift-Dump-Parser\pushshift-dump\RC_2011-08"
searchTerms = ["hand", "dryer"]

start = time.time()

linesSearched = 0
linesWithTerms = 0
with open(dumpPath, 'r') as f:
  for line in f:
    foundAllTerms = True
    for term in searchTerms:
        if term not in line:
            foundAllTerms = False
            break

    if foundAllTerms:
        linesWithTerms += 1
    linesSearched += 1

    end = time.time()
    if (end - start) >= 1:
        start = time.time()
        print(f"Searched: {linesSearched} Comments with terms: {linesWithTerms}")

