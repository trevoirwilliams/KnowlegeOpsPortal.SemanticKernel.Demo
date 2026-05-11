(() => {
    let conversationId = null;
    let hasLoadedHistory = false;

    const assistant = document.querySelector("[data-assistant]");
    const toggleButton = document.querySelector("[data-assistant-toggle]");

    if (!assistant || !toggleButton) {
        return;
    }

    const clearButton = assistant.querySelector("[data-assistant-clear]");
    const closeButton = assistant.querySelector("[data-assistant-close]");
    const form = assistant.querySelector("[data-assistant-form]");
    const input = assistant.querySelector("[data-assistant-input]");
    const messages = assistant.querySelector("[data-assistant-messages]");
    const submitButton = assistant.querySelector("[data-assistant-submit]");
    const status = assistant.querySelector("[data-assistant-status]");
    const contextLabel = assistant.querySelector("[data-assistant-context-label]");
    const tokenInput = assistant.querySelector("input[name='__RequestVerificationToken']");
    
    const endpoint = assistant.dataset.endpoint;
    const historyEndpoint = assistant.dataset.historyEndpoint;
    const clearEndpoint = assistant.dataset.clearEndpoint;


    if (!closeButton || !form || !input || !messages || !submitButton || !status || !contextLabel || !tokenInput || !endpoint) {
        console.error("The portal assistant is missing required elements.");
        return;
    }

    const pageContext = readPageContext();
    renderContextLabel(pageContext);

    toggleButton.addEventListener("click", async () => {
        const isOpening = assistant.classList.contains("d-none");

        assistant.classList.toggle("d-none", !isOpening);
        toggleButton.setAttribute("aria-expanded", String(isOpening));

        if (isOpening) {
            if (!hasLoadedHistory) {
                await loadHistory();
                hasLoadedHistory = true;
            }
            input.focus();
        }
    });

    closeButton.addEventListener("click", () => {
        assistant.classList.add("d-none");
        toggleButton.setAttribute("aria-expanded", "false");
        toggleButton.focus();
    });

    clearButton.addEventListener("click", async () => {
        if (!conversationId) {
            clearMessages();
            appendEmptyState();
            setStatus("Chat is clear.");
            return;
        }

        const confirmed = window.confirm("Clear this chat history for the current context?");

        if (!confirmed) {
            return;
        }

        setLoading(true);

        try {
            const response = await fetch(clearEndpoint, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "RequestVerificationToken": tokenInput.value
                },
                body: JSON.stringify({
                    conversationId,
                    context: pageContext
                })
            });

            if (!response.ok) {
                appendMessage("assistant", "The chat history could not be cleared.");
                return;
            }

            conversationId = null;
            clearMessages();
            appendEmptyState();
            setStatus("Chat history cleared.");
        } catch (error) {
            console.error("Could not clear assistant history.", error);
            appendMessage("assistant", "The chat history could not be cleared.");
        } finally {
            setLoading(false);
        }
    });

    document.addEventListener("click", (event) => {
        const suggestion = event.target.closest("[data-assistant-suggestion]");

        if (!suggestion) {
            return;
        }

        input.value = suggestion.dataset.assistantSuggestion ?? "";
        input.focus();
    });

    form.addEventListener("submit", async (event) => {
        event.preventDefault();

        const message = input.value.trim();

        if (message.length < 2) {
            setStatus("Please enter a more specific message.");
            return;
        }

        appendMessage("user", message);
        input.value = "";
        setLoading(true);

        try {
            const response = await fetch(endpoint, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "RequestVerificationToken": tokenInput.value
                },
                body: JSON.stringify({
                    message,
                    context: pageContext,
                    conversationId
                })
            });

            if (!response.ok) {
                await handleFailedResponse(response);
                return;
            }

            const data = await response.json();
            if (data.conversationId) {
                conversationId = data.conversationId;
            }
            appendMessage("assistant", data.message ?? "The assistant returned an empty response.");
        } catch (error) {
            console.error("Assistant request failed.", error);
            appendMessage("assistant", "The assistant could not be reached. Please try again.");
        } finally {
            setLoading(false);
        }
    });

    function readPageContext() {
        const contextElement = document.getElementById("assistant-page-context");

        if (!contextElement) {
            return null;
        }

        try {
            return JSON.parse(contextElement.textContent);
        } catch (error) {
            console.error("The assistant page context is invalid.", error);
            return null;
        }
    }

    function renderContextLabel(context) {
        if (!context || (!context.entityType && !context.pageTitle)) {
            contextLabel.classList.add("d-none");
            return;
        }

        const label = context.entityType && context.entityId
            ? `Context: ${context.entityType} ${context.entityId}`
            : `Context: ${context.pageTitle}`;

        contextLabel.textContent = label;
        contextLabel.classList.remove("d-none");
    }

    async function handleFailedResponse(response) {
        if (response.status === 400) {
            appendMessage("assistant", "Please check your message and try again.");
            return;
        }

        if (response.status === 403) {
            appendMessage("assistant", "The request could not be verified. Refresh the page and try again.");
            return;
        }

        const problem = await readJsonSafely(response);
        appendMessage("assistant", problem?.detail ?? "The assistant could not process the request.");
    }

    async function readJsonSafely(response) {
        try {
            return await response.json();
        } catch {
            return null;
        }
    }

    function appendMessage(role, text) {
        removeEmptyState();

        const item = document.createElement("article");
        item.className = role === "user"
            ? "portal-assistant-message portal-assistant-message-user"
            : "portal-assistant-message portal-assistant-message-assistant";

        const label = document.createElement("div");
        label.className = "portal-assistant-message-label";
        label.textContent = role === "user" ? "You" : "Assistant";

        const body = document.createElement("div");
        body.className = "portal-assistant-message-body";
        body.textContent = text;

        item.append(label, body);
        messages.appendChild(item);
        messages.scrollTop = messages.scrollHeight;
    }

    function removeEmptyState() {
        const emptyState = messages.querySelector(".portal-assistant-empty");

        if (emptyState) {
            emptyState.remove();
        }
    }

    function setLoading(isLoading) {
        input.disabled = isLoading;
        submitButton.disabled = isLoading;
        setStatus(isLoading ? "Assistant is thinking..." : "");
    }

    function setStatus(message) {
        status.textContent = message;
    }

    async function loadHistory() {
        setStatus("Loading conversation history...");

        try {
            const response = await fetch(historyEndpoint, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "RequestVerificationToken": tokenInput.value
                },
                body: JSON.stringify(pageContext)
            });

            if (!response.ok) {
                return;
            }

            const data = await response.json();

            conversationId = data.conversationId ?? null;

            if (Array.isArray(data.messages) && data.messages.length > 0) {
                clearMessages();

                for (const message of data.messages) {
                    appendMessage(message.role, message.content);
                }
            }
        } catch (error) {
            console.error("Could not load assistant history.", error);
        } finally {
            setStatus("");
        }
    }

    function clearMessages() {
        messages.innerHTML = "";
    }

    function appendEmptyState() {
        const emptyState = document.createElement("div");
        emptyState.className = "portal-assistant-empty";
        emptyState.innerHTML = `
            <p class="fw-semibold mb-1">How can I help?</p>
            <p class="small text-muted mb-0">
                Ask a question about this page to get started.
            </p>
        `;
        messages.appendChild(emptyState);
    }

})();