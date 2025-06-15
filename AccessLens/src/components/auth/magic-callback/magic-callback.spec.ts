import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MagicCallback } from './magic-callback';

describe('MagicCallback', () => {
  let component: MagicCallback;
  let fixture: ComponentFixture<MagicCallback>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MagicCallback]
    })
    .compileComponents();

    fixture = TestBed.createComponent(MagicCallback);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
