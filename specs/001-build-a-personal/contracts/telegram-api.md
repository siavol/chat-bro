# Telegram Bot API Contracts

This document defines the contract for the Telegram bot API.

## User to Bot

### /start
- **Description**: Initiates the conversation with the bot.
- **Payload**: N/A

### /help
- **Description**: Provides help information.
- **Payload**: N/A

### Text Message
- **Description**: A plain text message from the user. The bot will interpret this as a question or a request for a lunch recommendation.
- **Payload**: string

## Bot to User

### Text Message
- **Description**: A plain text message from the bot to the user. This can be an answer to a question or a lunch recommendation.
- **Payload**: string
