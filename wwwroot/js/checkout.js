document.addEventListener("DOMContentLoaded", function () {
    // --- SECTION 1: Element Selectors ---
    const startTimeInput = document.getElementById("startTime");
    const pricePerKwhInput = document.getElementById("pricePerKwh");
    const chargerPowerKwInput = document.getElementById("chargerPowerKw");
    const bookingDateInput = document.getElementById("bookingDate");
    
    // Sliders & Number inputs
    const inputBatteryCapacity = document.getElementById("inputBatteryCapacity");
    const inputBatteryCapacityManual = document.getElementById("inputBatteryCapacityManual");
    const sliderCurrentCharge = document.getElementById("sliderCurrentCharge");
    const sliderTargetCharge = document.getElementById("sliderTargetCharge");
    
    // UI Display elements
    const badgeBatteryDisplay = document.getElementById("badgeBatteryDisplay");
    const badgeCurrentChargeDisplay = document.getElementById("badgeCurrentChargeDisplay");
    const badgeTargetChargeDisplay = document.getElementById("badgeTargetChargeDisplay");
    const warningEvBattery = document.getElementById("warningEvBattery");
    
    const displayEnergyToDeliver = document.getElementById("displayEnergyToDeliver");
    const displayChargingDuration = document.getElementById("displayChargingDuration");
    const displayEndTime = document.getElementById("displayEndTime");
    const liveCalculatedPriceSpan = document.getElementById("liveCalculatedPrice");

    // Sidebar & Locks
    const btnVerifyAndProceed = document.getElementById("btnVerifyAndProceed");
    const btnCloseSidebar = document.getElementById("btnCloseSidebar");
    const stripeSidebar = document.getElementById("stripeSidebar");

    const summaryDuration = document.getElementById("summaryDuration");
    const summaryTotal = document.getElementById("summaryTotal");

    const kwhRate = parseFloat(pricePerKwhInput.value) || 0.12;
    const chargerPowerKw = parseFloat(chargerPowerKwInput.value) || 7.4;

    // Attach click listener to close sidebar
    if (btnCloseSidebar) {
        btnCloseSidebar.addEventListener("click", function () {
            stripeSidebar.style.display = "none";
            document.body.classList.remove("show-sidebar");
        });
    }

    // --- SECTION 2: EV Dynamic Live Price & Time Calculator ---
    function calculateLivePrice() {
        const capacity = parseFloat(inputBatteryCapacity.value) || 60;
        const currentPct = parseInt(sliderCurrentCharge.value) || 0;
        const targetPct = parseInt(sliderTargetCharge.value) || 100;
        const startVal = startTimeInput.value;

        // Update Slider displays
        badgeBatteryDisplay.innerText = `${capacity} kWh`;
        badgeCurrentChargeDisplay.innerText = `${currentPct}%`;
        badgeTargetChargeDisplay.innerText = `${targetPct}%`;

        // 1. Calculate Energy to Deliver (kWh)
        const energyNeededKwh = capacity * (targetPct - currentPct) / 100.0;
        displayEnergyToDeliver.innerText = `${energyNeededKwh.toFixed(2)} kWh`;

        // 2. Charging speed efficiency: AC ~ 90%, DC ~ 95%
        const efficiency = chargerPowerKw > 22.0 ? 0.95 : 0.90;
        const durationHours = energyNeededKwh / (chargerPowerKw * efficiency);

        // Convert duration to hours/minutes for display
        const totalMinutes = Math.round(durationHours * 60);
        const hours = Math.floor(totalMinutes / 60);
        const minutes = totalMinutes % 60;
        displayChargingDuration.innerText = `${hours}h ${minutes}m`;

        // 3. Auto-Calculate End Time
        if (startVal) {
            const todayStr = new Date().toISOString().split('T')[0];
            const startDateTime = new Date(`${todayStr}T${startVal}`);
            const durationMs = durationHours * 60 * 60 * 1000;
            const endDateTime = new Date(startDateTime.getTime() + durationMs);
            
            const endHours = String(endDateTime.getHours()).padStart(2, '0');
            const endMinutes = String(endDateTime.getMinutes()).padStart(2, '0');
            displayEndTime.innerText = `${endHours}:${endMinutes}`;
        } else {
            displayEndTime.innerText = "--:--";
        }

        // 4. Calculate Bill Total (JOD)
        const totalCostJod = energyNeededKwh * kwhRate;
        liveCalculatedPriceSpan.innerText = totalCostJod.toFixed(2);

        // 5. Educational Warning for > 80% SoC on Fast Chargers
        if (targetPct > 80) {
            warningEvBattery.style.display = "block";
        } else {
            warningEvBattery.style.display = "none";
        }

        // Sync payment sidebar summary details
        if (summaryDuration) {
            summaryDuration.innerText = `${energyNeededKwh.toFixed(1)} kWh`;
        }
        if (summaryTotal) {
            summaryTotal.innerText = totalCostJod.toFixed(2);
        }
    }

    // --- SECTION 3: Event Listeners for Live Math Reactivity ---
    inputBatteryCapacity.addEventListener("input", function() {
        inputBatteryCapacityManual.value = this.value;
        calculateLivePrice();
    });

    inputBatteryCapacityManual.addEventListener("input", function() {
        let val = parseInt(this.value);
        if (isNaN(val)) return;
        if (val < 10) val = 10;
        if (val > 150) val = 150;
        inputBatteryCapacity.value = val;
        calculateLivePrice();
    });

    sliderCurrentCharge.addEventListener("input", function() {
        const currentVal = parseInt(this.value);
        const targetVal = parseInt(sliderTargetCharge.value);
        if (targetVal <= currentVal) {
            sliderTargetCharge.value = Math.min(100, currentVal + 5);
            badgeTargetChargeDisplay.innerText = `${sliderTargetCharge.value}%`;
        }
        calculateLivePrice();
    });

    sliderTargetCharge.addEventListener("input", function() {
        const targetVal = parseInt(this.value);
        const currentVal = parseInt(sliderCurrentCharge.value);
        if (targetVal <= currentVal) {
            this.value = Math.min(100, currentVal + 5);
        }
        calculateLivePrice();
    });

    startTimeInput.addEventListener("input", calculateLivePrice);
    
    // Run initial live calculation on page load
    calculateLivePrice();

    // --- SECTION 4: AJAX Intent Trigger & Sidebar Slide Activation ---
    btnVerifyAndProceed.addEventListener("click", function () {
        const form = document.getElementById("bookingDataForm");
        if (!form.checkValidity()) {
            form.reportValidity();
            return;
        }

        const startVal = startTimeInput.value;
        if (!startVal) {
            Swal.fire({
                icon: 'warning',
                title: 'Start Time Required',
                text: 'Please pick a valid start time.',
                confirmButtonColor: '#10b981'
            });
            return;
        }

        const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

        const bookingRequestData = {
            chargerSlotId: parseInt(document.getElementById("chargerSlotId").value),
            bookingDate: bookingDateInput.value,
            startTime: startVal + ":00",
            batteryCapacityKwh: parseFloat(inputBatteryCapacity.value),
            currentChargePct: parseFloat(sliderCurrentCharge.value),
            targetChargePct: parseFloat(sliderTargetCharge.value)
        };

        // Disable button to prevent double-submit
        btnVerifyAndProceed.disabled = true;
        const originalBtnHtml = btnVerifyAndProceed.innerHTML;
        btnVerifyAndProceed.innerHTML = `<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span> Locking Slot & Contacting Stripe...`;

        fetch("/Bookings/CreatePaymentIntent", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "RequestVerificationToken": token
            },
            body: JSON.stringify(bookingRequestData)
        })
        .then(async response => {
            if (!response.ok) {
                let msg = "Request failed with status " + response.status;
                try {
                    const err = await response.json();
                    msg = err.message || msg;
                } catch (_) {}
                throw new Error(msg);
            }
            return response.json();
        })
        .then(data => {
            // SUCCESSFUL PIPELINE: Server locked slot.
            // Reveal checkout sidebar
            stripeSidebar.style.display = "block";
            document.body.classList.add("show-sidebar");

            // Initialize Secure Stripe Card input
            initializeStripePaymentEngine(data.clientSecret, data.bookingId);
            
            // Highlight locked state
            btnVerifyAndProceed.innerHTML = `Charging Slot Locked <i class="bi bi-shield-fill-check ms-2"></i>`;
            btnVerifyAndProceed.classList.remove("btn-premium-home");
            btnVerifyAndProceed.classList.add("btn-success");
        })
        .catch(error => {
            Swal.fire({
                icon: 'error',
                title: 'Reservation Failed',
                text: error.message,
                confirmButtonColor: '#10b981'
            });
            btnVerifyAndProceed.disabled = false;
            btnVerifyAndProceed.innerHTML = originalBtnHtml;
        });
        // Setup form submit listener ONCE on page load
        const stripePaymentForm = document.getElementById("stripePaymentForm");
        if (stripePaymentForm) {
            stripePaymentForm.addEventListener("submit", function(event) {
                event.preventDefault();
                
                if (!stripeInstance || !cardElementInstance || !currentClientSecret || !currentBookingId) {
                    Swal.fire({
                        icon: 'error',
                        title: 'Initialization Error',
                        text: 'Stripe engine not fully initialized. Please reload and try again.',
                        confirmButtonColor: '#10b981'
                    });
                    return;
                }
                
                const btnSubmit = document.getElementById("btnSubmitPayment");
                const spinner = document.getElementById("paymentSpinner");
                const cardErrors = document.getElementById("cardErrors");
                
                btnSubmit.disabled = true;
                spinner.classList.remove("d-none");
                cardErrors.textContent = "";
                
                stripeInstance.confirmCardPayment(currentClientSecret, {
                    payment_method: {
                        card: cardElementInstance
                    }
                }).then(function(result) {
                    if (result.error) {
                        cardErrors.textContent = result.error.message;
                        btnSubmit.disabled = false;
                        spinner.classList.add("d-none");
                    } else {
                        if (result.paymentIntent.status === 'succeeded') {
                            const verificationToken = document.querySelector('input[name="__RequestVerificationToken"]').value;
                            
                            fetch("/Bookings/ConfirmPayment", {
                                method: "POST",
                                headers: {
                                    "Content-Type": "application/json",
                                    "RequestVerificationToken": verificationToken
                                },
                                body: JSON.stringify({
                                    bookingId: currentBookingId,
                                    paymentIntentId: result.paymentIntent.id
                                })
                            })
                            .then(async response => {
                                if (!response.ok) {
                                    let msg = "Confirmation failed.";
                                    try {
                                        const err = await response.json();
                                        msg = err.message || msg;
                                    } catch(_) {}
                                    throw new Error(msg);
                                }
                                return response.json();
                            })
                            .then(data => {
                                Swal.fire({
                                    icon: 'success',
                                    title: '⚡ Booking Confirmed!',
                                    text: 'Thank you! Your EV smart charging booking has been confirmed successfully!',
                                    confirmButtonColor: '#10b981'
                                }).then(() => {
                                    window.location.href = "/Map";
                                });
                            })
                            .catch(err => {
                                Swal.fire({
                                    icon: 'error',
                                    title: 'Confirmation Failed',
                                    text: 'Payment succeeded but server booking confirmation failed: ' + err.message,
                                    confirmButtonColor: '#10b981'
                                });
                                btnSubmit.disabled = false;
                                spinner.classList.add("d-none");
                            });
                        }
                    }
                });
            });
        }
    });
});

// Global references to prevent duplicate instances
let stripeInstance = null;
let cardElementInstance = null;
let currentClientSecret = null;
let currentBookingId = null;

// Secure Stripe Card Initializer
function initializeStripePaymentEngine(clientSecret, bookingId) {
    currentClientSecret = clientSecret;
    currentBookingId = bookingId;

    if (!stripeInstance) {
        const stripeCardElement = document.getElementById("stripeCardElement");
        const publishableKey = stripeCardElement ? stripeCardElement.getAttribute("data-publishable-key") : null;
        if (publishableKey) {
            stripeInstance = Stripe(publishableKey);
        } else {
            console.error("Stripe Publishable Key not found on stripeCardElement.");
        }
    }
    
    const elements = stripeInstance.elements();
    
    const style = {
        base: {
            color: "#212529",
            fontFamily: "'Outfit', -apple-system, sans-serif",
            fontSmoothing: "antialiased",
            fontSize: "16px",
            "::placeholder": {
                color: "#6c757d"
            },
            lineHeight: "24px"
        },
        invalid: {
            color: "#dc3545",
            iconColor: "#dc3545"
        }
    };
    
    const stripeContainer = document.getElementById("stripeCardElement");
    stripeContainer.innerHTML = "";
    
    if (cardElementInstance) {
        cardElementInstance.destroy();
    }
    
    cardElementInstance = elements.create("card", { 
        style: style,
        hidePostalCode: true
    });
    
    cardElementInstance.mount("#stripeCardElement");
    
    cardElementInstance.on('change', function(event) {
        const displayError = document.getElementById('cardErrors');
        if (event.error) {
            displayError.textContent = event.error.message;
        } else {
            displayError.textContent = '';
        }
    });
}