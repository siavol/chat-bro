# Estimator Agent

This is an idea of a new domain agents to the chat bro. The user flow could be

1. User writes in a chat that he want to find smth, lets say bike. User writes what is important, criteria use cases etc.
  - AI agent identifies parameters which will be used to estimate every candidate.
  - For each parameter it describes what does it mean and how to pick the value. It will be an instruction for future agent sessions. Should be stored in the agent context storage.
  - AI agent creates a Google Sheet with parameters and formula to calculate item score, which illustrate how good this item satisfies users requirements and how good is this buy.

2. User sends to the chat link for the buy candidate.
  - AI agent extracts item information from the web link. It could be page to the second hand market and when page has not full information about the item, agent can search for the additional information in the web.
  - AI agent estimate parameter values for this item
  - AI agent adds this information to the Google Sheet as a new row.
  - The item score value is calculated automatically by the Sheet document according to the formula

3. AI agent responds with the item overview, including item score, highlights some pros and cons of this item.
