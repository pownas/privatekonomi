(() => {
    const fromBase64Url = value => {
        const base64 = value.replace(/-/g, '+').replace(/_/g, '/');
        const padded = base64 + '='.repeat((4 - base64.length % 4) % 4);
        return Uint8Array.from(atob(padded), character => character.charCodeAt(0)).buffer;
    };

    const toBase64Url = buffer => {
        const bytes = new Uint8Array(buffer);
        let binary = '';
        for (const byte of bytes) {
            binary += String.fromCharCode(byte);
        }
        return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/g, '');
    };

    const prepareOptions = (options, creation) => {
        options.challenge = fromBase64Url(options.challenge);
        if (creation) {
            options.user.id = fromBase64Url(options.user.id);
            for (const credential of options.excludeCredentials ?? []) {
                credential.id = fromBase64Url(credential.id);
            }
        } else {
            for (const credential of options.allowCredentials ?? []) {
                credential.id = fromBase64Url(credential.id);
            }
        }
        return options;
    };

    const serializeCredential = credential => {
        const response = credential.response;
        const serializedResponse = {
            clientDataJSON: toBase64Url(response.clientDataJSON)
        };

        if ('attestationObject' in response) {
            serializedResponse.attestationObject = toBase64Url(response.attestationObject);
        } else {
            serializedResponse.authenticatorData = toBase64Url(response.authenticatorData);
            serializedResponse.signature = toBase64Url(response.signature);
            serializedResponse.userHandle = response.userHandle ? toBase64Url(response.userHandle) : null;
            serializedResponse.transports = response.getTransports?.() ?? [];
        }

        return JSON.stringify({
            id: credential.id,
            rawId: toBase64Url(credential.rawId),
            type: credential.type,
            response: serializedResponse,
            authenticatorAttachment: credential.authenticatorAttachment
        });
    };

    const showMessage = (elementId, message) => {
        const element = document.getElementById(elementId);
        if (element) {
            element.textContent = message;
            element.style.display = '';
        }
    };

    const getToken = form => form.querySelector('input[name="__RequestVerificationToken"]')?.value;

    const post = async (url, token, body) => {
        const response = await fetch(url, {
            method: 'POST',
            credentials: 'same-origin',
            headers: {
                'RequestVerificationToken': token,
                ...(body ? { 'Content-Type': 'application/json' } : {})
            },
            body: body ? JSON.stringify(body) : undefined
        });
        if (!response.ok) {
            throw new Error(await response.text() || `Serverfel (${response.status}).`);
        }
        return response;
    };

    const supportsPasskeys = () =>
        window.isSecureContext &&
        typeof navigator.credentials?.get === 'function' &&
        typeof navigator.credentials?.create === 'function' &&
        typeof window.PublicKeyCredential !== 'undefined';

    const setUnavailableMessage = () => {
        const loginButton = document.getElementById('passkey-login');
        if (loginButton) {
            loginButton.disabled = true;
            showMessage('passkey-error', 'Passkeys kräver en säker anslutning (HTTPS) och en webbläsare med WebAuthn-stöd.');
        }
    };

    document.addEventListener('click', async event => {
        const button = event.target.closest('#passkey-login, #passkey-create');
        if (!button) {
            return;
        }

        event.preventDefault();
        const form = button.closest('form') ?? document.querySelector('#passkey-create-form, form[action="/Account/PerformLogin"]');
        const token = form && getToken(form);
        const login = button.id === 'passkey-login';
        const errorId = login ? 'passkey-error' : 'passkey-create-error';
        const successId = 'passkey-create-success';

        document.getElementById(errorId)?.style && (document.getElementById(errorId).style.display = 'none');
        document.getElementById(successId)?.style && (document.getElementById(successId).style.display = 'none');

        if (!supportsPasskeys()) {
            setUnavailableMessage();
            return;
        }
        if (!token) {
            showMessage(errorId, 'Säkerhetskontrollen saknas. Ladda om sidan och försök igen.');
            return;
        }

        button.disabled = true;
        try {
            const optionsUrl = login
                ? '/Account/PasskeyRequestOptions'
                : '/Account/Manage/PasskeyCreationOptions';
            const optionsResponse = await post(optionsUrl, token);
            const options = prepareOptions(await optionsResponse.json(), !login);
            const credential = login
                ? await navigator.credentials.get({ publicKey: options })
                : await navigator.credentials.create({ publicKey: options });

            if (!credential) {
                throw new Error('Ingen passkey valdes.');
            }

            const credentialJson = serializeCredential(credential);
            if (login) {
                const response = await post('/Account/PerformPasskeyLogin', token, {
                    credentialJson,
                    returnUrl: button.dataset.returnUrl
                });
                const result = await response.json();
                window.location.assign(result.redirectUrl);
            } else {
                await post('/Account/Manage/PerformPasskeyCreation', token, { credentialJson });
                window.location.reload();
            }
        } catch (error) {
            if (error.name !== 'NotAllowedError' && error.name !== 'AbortError') {
                showMessage(errorId, error.message || 'Det gick inte att använda passkey.');
            } else if (error.name === 'NotAllowedError') {
                showMessage(errorId, 'Ingen passkey valdes eller autentiseringen avbröts.');
            }
        } finally {
            button.disabled = false;
        }
    });

    if (!supportsPasskeys()) {
        setUnavailableMessage();
    }
})();
